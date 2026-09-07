using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Payments.Approve;

[Collection<OrdersApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Payments")]
public sealed class ApprovePaymentEndpointTests(OrderFlowIntegrationSuiteFixture environment)
    : FeatureApiTest(environment, IntegrationFeatures.Orders)
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_Confirm_Order_Without_Changing_Inventory_Or_Historical_Data()
    {
        var order = await CreatePendingOrderAsync();
        var productId = order.Items.Single().ProductId;
        var before = await TestData.GetInventoryItemByProductIdAsync(productId);
        await using (var scope = Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            var product = await db.Products.SingleAsync(product => product.Id == productId, CancellationToken);
            product.ChangePrice(200);
            product.SetStatus(false);
            await db.SaveChangesAsync(CancellationToken);
        }

        using var response = await ApproveAsync(order.Id);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken);
        Assert.Equal("Confirmed", body!.Status);
        using var lookup = await Client.GetAsync($"/orders/{order.Id}", CancellationToken);
        var persisted = await lookup.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken);
        Assert.Equal("Confirmed", persisted!.Status);
        Assert.Equal(order.Id, persisted.Id);
        Assert.Equal(order.TotalAmount, persisted.TotalAmount);
        Assert.InRange(persisted.CreatedAt, order.CreatedAt.AddMicroseconds(-1), order.CreatedAt.AddMicroseconds(1));
        Assert.Null(persisted.CancelledAt);
        Assert.Equal(order.Items.ToArray(), persisted.Items.ToArray());
        var after = await TestData.GetInventoryItemByProductIdAsync(productId);
        Assert.Equal(before!.Quantity, after!.Quantity);
        Assert.Equal(before.ReservedQuantity, after.ReservedQuantity);
        Assert.Equal(before.AvailableQuantity, after.AvailableQuantity);
        Assert.Equal(before.UpdatedAt, after.UpdatedAt);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        using var response = await ApproveAsync(Guid.NewGuid());
        await AssertErrorAsync(response, HttpStatusCode.NotFound, "order.not_found");
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    public async Task Should_Reject_Approval_When_Order_Is_Not_Pending(OrderStatus status)
    {
        var order = await CreatePendingOrderAsync();
        if (status == OrderStatus.Confirmed)
        {
            using var firstApproval = await ApproveAsync(order.Id);
            firstApproval.EnsureSuccessStatusCode();
        }
        else if (status == OrderStatus.Cancelled)
        {
            using var cancellation = await CancelAsync(order.Id);
            cancellation.EnsureSuccessStatusCode();
        }
        else
        {
            using var rejection = await RejectAsync(order.Id);
            rejection.EnsureSuccessStatusCode();
        }

        using var response = await ApproveAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "payment.order_not_pending");
        await AssertPersistedStateAsync(order.Id, status, status == OrderStatus.Confirmed ? 2 : 0);
    }

    [Fact]
    public async Task Should_Reject_Cancellation_After_Approval_Without_Releasing_Inventory()
    {
        var order = await CreatePendingOrderAsync();
        using var approval = await ApproveAsync(order.Id);
        approval.EnsureSuccessStatusCode();

        using var response = await CancelAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "order.cannot_cancel");
        await AssertPersistedStateAsync(order.Id, OrderStatus.Confirmed, 2);
    }

    [Fact]
    public async Task Should_Allow_Only_One_Concurrent_Approval()
    {
        var order = await CreatePendingOrderAsync();
        var responses = await Task.WhenAll(ApproveAsync(order.Id), ApproveAsync(order.Id));
        try
        {
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            await AssertErrorAsync(conflict, HttpStatusCode.Conflict, "payment.order_not_pending");
            await AssertPersistedStateAsync(order.Id, OrderStatus.Confirmed, 2);
        }
        finally
        {
            foreach (var response in responses) response.Dispose();
        }
    }

    [Fact]
    public async Task Should_Allow_Only_One_Transition_When_Approval_Competes_With_Cancellation()
    {
        var order = await CreatePendingOrderAsync();
        var responses = await Task.WhenAll(ApproveAsync(order.Id), CancelAsync(order.Id));
        using var approval = responses[0];
        using var cancellation = responses[1];
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        if (approval.IsSuccessStatusCode)
        {
            await AssertErrorAsync(cancellation, HttpStatusCode.Conflict, "order.cannot_cancel");
            await AssertPersistedStateAsync(order.Id, OrderStatus.Confirmed, 2);
        }
        else
        {
            await AssertErrorAsync(approval, HttpStatusCode.Conflict, "payment.order_not_pending");
            await AssertPersistedStateAsync(order.Id, OrderStatus.Cancelled, 0);
        }
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

    private async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        var problem = await response.ReadProblemDetailsAsync(CancellationToken);
        Assert.Equal((int)status, problem!.Status);
        Assert.Equal(code, problem.Extensions["code"]?.ToString());
    }

    private async Task AssertPersistedStateAsync(Guid orderId, OrderStatus status, int reserved)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var order = await db.Orders.SingleAsync(value => value.Id == orderId, CancellationToken);
        var inventory = await db.InventoryItems.SingleAsync(CancellationToken);
        Assert.Equal(status, order.Status);
        Assert.Equal(status == OrderStatus.Cancelled, order.CancelledAt.HasValue);
        Assert.Equal(5, inventory.Quantity);
        Assert.Equal(reserved, inventory.ReservedQuantity);
        Assert.Equal(5 - reserved, inventory.AvailableQuantity);
    }
}
