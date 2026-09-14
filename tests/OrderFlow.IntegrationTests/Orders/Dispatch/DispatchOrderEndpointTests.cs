using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.InventoryItems.Increase;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Contracts.Events.Orders;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Orders.Dispatch;

[Collection<OrdersApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Orders")]
public sealed class DispatchOrderEndpointTests(OrderFlowIntegrationSuiteFixture environment)
    : FeatureApiTest(environment, IntegrationFeatures.Orders)
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_Dispatch_Confirmed_Order_And_Deduct_Reserved_Inventory()
    {
        var order = await CreateConfirmedOrderAsync();
        var productId = order.Items.Single().ProductId;

        using var response = await DispatchAsync(order.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken);
        Assert.Equal("Dispatched", body!.Status);
        Assert.NotNull(body.DispatchedAt);
        Assert.Equal(order.TotalAmount, body.TotalAmount);
        Assert.InRange(body.CreatedAt, order.CreatedAt.AddMicroseconds(-1), order.CreatedAt.AddMicroseconds(1));
        Assert.Null(body.CancelledAt);
        Assert.Equal(order.Items.ToArray(), body.Items.ToArray());
        await AssertPersistedStateAsync(order.Id, productId, OrderStatus.Dispatched, 3, 0);
        var outboxMessage = Assert.Single(await GetOutboxMessagesAsync());
        Assert.NotEqual(Guid.Empty, outboxMessage.Id);
        Assert.Equal("order.dispatched.v1", outboxMessage.EventType);
        Assert.Equal(DateTimeKind.Utc, outboxMessage.CreatedAt.Kind);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(0, outboxMessage.AttemptCount);
        Assert.Null(outboxMessage.LastAttemptedAt);
        Assert.Null(outboxMessage.LastError);
        var dispatchedEvent = JsonSerializer.Deserialize<OrderDispatched>(outboxMessage.Payload, JsonSerializerOptions.Web);
        Assert.NotNull(dispatchedEvent);
        Assert.Equal(order.Id, dispatchedEvent.OrderId);
        Assert.Equal(body.DispatchedAt!.Value, dispatchedEvent.DispatchedAtUtc);
        var dispatchedItem = Assert.Single(dispatchedEvent.Items);
        Assert.Equal(productId, dispatchedItem.ProductId);
        Assert.Equal(2, dispatchedItem.Quantity);

        using var lookup = await Client.GetAsync($"/orders/{order.Id}", CancellationToken);
        var persisted = await lookup.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken);
        Assert.Equal("Dispatched", persisted!.Status);
        Assert.NotNull(persisted.DispatchedAt);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        using var response = await DispatchAsync(Guid.NewGuid());

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "order.not_found");
        await AssertOutboxMessageCountAsync(0);
    }

    [Theory]
    [InlineData(OrderStatus.PendingPayment)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    [InlineData(OrderStatus.Dispatched)]
    public async Task Should_Return_Conflict_When_Order_Is_Not_Confirmed(OrderStatus status)
    {
        var order = await CreatePendingOrderAsync();
        var productId = order.Items.Single().ProductId;
        if (status == OrderStatus.Cancelled)
        {
            using var cancellation = await CancelAsync(order.Id);
            cancellation.EnsureSuccessStatusCode();
        }
        else if (status == OrderStatus.PaymentRejected)
        {
            using var rejection = await RejectAsync(order.Id);
            rejection.EnsureSuccessStatusCode();
        }
        else if (status == OrderStatus.Dispatched)
        {
            using var approval = await ApproveAsync(order.Id);
            approval.EnsureSuccessStatusCode();
            using var dispatch = await DispatchAsync(order.Id);
            dispatch.EnsureSuccessStatusCode();
        }

        using var response = await DispatchAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "order.cannot_dispatch");
        await AssertPersistedStateAsync(
            order.Id,
            productId,
            status,
            status == OrderStatus.Dispatched ? 3 : 5,
            status == OrderStatus.PendingPayment ? 2 : 0);
        await AssertOutboxMessageCountAsync(status == OrderStatus.Dispatched ? 1 : 0);
    }

    [Fact]
    public async Task Should_Not_Dispatch_When_Reserved_Inventory_Is_Inconsistent()
    {
        var order = await CreateConfirmedOrderAsync();
        var productId = order.Items.Single().ProductId;
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE inventory_items
                SET reserved_quantity = 1
                WHERE product_id = {productId}
                """, CancellationToken);
        }

        using var response = await DispatchAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "order.inventory_inconsistent");
        await AssertPersistedStateAsync(order.Id, productId, OrderStatus.Confirmed, 5, 1);
        await AssertOutboxMessageCountAsync(0);
    }

    [Fact]
    public async Task Should_Reject_Cancellation_After_Dispatch()
    {
        var order = await CreateConfirmedOrderAsync();
        using var dispatch = await DispatchAsync(order.Id);
        dispatch.EnsureSuccessStatusCode();

        using var response = await CancelAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "order.cannot_cancel");
    }

    [Fact]
    public async Task Should_Allow_Only_One_Concurrent_Dispatch()
    {
        var order = await CreateConfirmedOrderAsync();
        var productId = order.Items.Single().ProductId;
        var responses = await Task.WhenAll(DispatchAsync(order.Id), DispatchAsync(order.Id));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            await AssertErrorAsync(conflict, HttpStatusCode.Conflict, "order.cannot_dispatch");
            await AssertPersistedStateAsync(order.Id, productId, OrderStatus.Dispatched, 3, 0);
            await AssertOutboxMessageCountAsync(1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Should_Preserve_Dispatch_And_Concurrent_Stock_Increase()
    {
        var order = await CreateConfirmedOrderAsync();
        var productId = order.Items.Single().ProductId;
        var responses = await Task.WhenAll(
            DispatchAsync(order.Id),
            Client.PostAsJsonAsync(
                $"/inventory/items/{productId}/increase",
                new IncreaseInventoryItemStockRequest(5),
                CancellationToken));
        try
        {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
            await AssertPersistedStateAsync(order.Id, productId, OrderStatus.Dispatched, 8, 0);
            await AssertOutboxMessageCountAsync(1);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    private async Task<OrderResponse> CreateConfirmedOrderAsync()
    {
        var order = await CreatePendingOrderAsync();
        using var approval = await ApproveAsync(order.Id);
        approval.EnsureSuccessStatusCode();
        return order;
    }

    private async Task<OrderResponse> CreatePendingOrderAsync()
    {
        var product = await TestData.Product().WithPrice(100).PersistAsync(CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(5).PersistAsync(CancellationToken);
        using var response = await Client.PostAsJsonAsync("/orders",
            new CreateOrderRequest([new CreateOrderItemRequest(product.Id, 2)]), CancellationToken);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken))!;
    }

    private Task<HttpResponseMessage> ApproveAsync(Guid orderId) =>
        Client.PostAsync($"/payments/{orderId}/approve", null, CancellationToken);

    private Task<HttpResponseMessage> RejectAsync(Guid orderId) =>
        Client.PostAsync($"/payments/{orderId}/reject", null, CancellationToken);

    private Task<HttpResponseMessage> CancelAsync(Guid orderId) =>
        Client.PostAsync($"/orders/{orderId}/cancel", null, CancellationToken);

    private Task<HttpResponseMessage> DispatchAsync(Guid orderId) =>
        Client.PostAsync($"/orders/{orderId}/dispatch", null, CancellationToken);

    private async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.ReadProblemDetailsAsync(CancellationToken);
        Assert.Equal((int)status, problem!.Status);
        Assert.Equal(code, problem.Extensions["code"]?.ToString());
    }

    private async Task AssertPersistedStateAsync(
        Guid orderId,
        Guid productId,
        OrderStatus status,
        int quantity,
        int reservedQuantity)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var order = await db.Orders.SingleAsync(value => value.Id == orderId, CancellationToken);
        var inventory = await db.InventoryItems.SingleAsync(value => value.ProductId == productId, CancellationToken);
        Assert.Equal(status, order.Status);
        Assert.Equal(status == OrderStatus.Dispatched, order.DispatchedAt.HasValue);
        Assert.Equal(quantity, inventory.Quantity);
        Assert.Equal(reservedQuantity, inventory.ReservedQuantity);
        Assert.Equal(quantity - reservedQuantity, inventory.AvailableQuantity);
    }

    private async Task AssertOutboxMessageCountAsync(int expectedCount)
    {
        var messages = await GetOutboxMessagesAsync();

        Assert.Equal(expectedCount, messages.Count);
    }

    private async Task<IReadOnlyList<OutboxMessageRow>> GetOutboxMessagesAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await db.Database.OpenConnectionAsync(CancellationToken);

        try
        {
            using var command = db.Database.GetDbConnection().CreateCommand();
            command.CommandText = """
                SELECT id, event_type, payload::text, created_at, processed_at, attempt_count, last_attempted_at, last_error
                FROM outbox_messages
                ORDER BY created_at
                """;
            await using var reader = await command.ExecuteReaderAsync(CancellationToken);
            var messages = new List<OutboxMessageRow>();

            while (await reader.ReadAsync(CancellationToken))
            {
                messages.Add(new OutboxMessageRow(
                    reader.GetGuid(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    reader.GetDateTime(3),
                    reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                    reader.GetInt32(5),
                    reader.IsDBNull(6) ? null : reader.GetDateTime(6),
                    reader.IsDBNull(7) ? null : reader.GetString(7)));
            }

            return messages;
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    private sealed record OutboxMessageRow(
        Guid Id,
        string EventType,
        string Payload,
        DateTime CreatedAt,
        DateTime? ProcessedAt,
        int AttemptCount,
        DateTime? LastAttemptedAt,
        string? LastError);
}
