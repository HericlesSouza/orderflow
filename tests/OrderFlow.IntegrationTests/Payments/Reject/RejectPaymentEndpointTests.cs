using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Payments.Reject;

[Collection<OrdersApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Payments")]
public sealed class RejectPaymentEndpointTests(OrderFlowIntegrationSuiteFixture environment)
    : FeatureApiTest(environment, IntegrationFeatures.Orders)
{
    private CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Should_Reject_Order_And_Release_Reserved_Inventory()
    {
        var order = await CreatePendingOrderAsync();

        using var response = await RejectAsync(order.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(CancellationToken);
        Assert.Equal("PaymentRejected", body!.Status);
        Assert.Equal(order.TotalAmount, body.TotalAmount);
        Assert.InRange(body.CreatedAt, order.CreatedAt.AddMicroseconds(-1), order.CreatedAt.AddMicroseconds(1));
        Assert.Null(body.CancelledAt);
        Assert.Equal(order.Items.ToArray(), body.Items.ToArray());
        await AssertPersistedStateAsync(order.Id, OrderStatus.PaymentRejected, 0);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        using var response = await RejectAsync(Guid.NewGuid());

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "order.not_found");
    }

    [Theory]
    [InlineData(OrderStatus.Confirmed)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.PaymentRejected)]
    public async Task Should_Return_Conflict_Without_Releasing_Inventory_Twice_When_Order_Is_Not_Pending(OrderStatus status)
    {
        var order = await CreatePendingOrderAsync();
        if (status == OrderStatus.Confirmed)
        {
            using var approval = await ApproveAsync(order.Id);
            approval.EnsureSuccessStatusCode();
        }
        else if (status == OrderStatus.Cancelled)
        {
            using var cancellation = await CancelAsync(order.Id);
            cancellation.EnsureSuccessStatusCode();
        }
        else
        {
            using var firstRejection = await RejectAsync(order.Id);
            firstRejection.EnsureSuccessStatusCode();
        }

        using var response = await RejectAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "payment.order_not_pending");
        await AssertPersistedStateAsync(order.Id, status, status == OrderStatus.Confirmed ? 2 : 0);
    }

    [Fact]
    public async Task Should_Reject_Cancellation_After_Payment_Rejection()
    {
        var order = await CreatePendingOrderAsync();
        using var rejection = await RejectAsync(order.Id);
        rejection.EnsureSuccessStatusCode();

        using var response = await CancelAsync(order.Id);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "order.cannot_cancel");
        await AssertPersistedStateAsync(order.Id, OrderStatus.PaymentRejected, 0);
    }

    [Fact]
    public async Task Should_Allow_Only_One_Transition_When_Rejection_Competes_With_Approval()
    {
        var order = await CreatePendingOrderAsync();
        var responses = await Task.WhenAll(RejectAsync(order.Id), ApproveAsync(order.Id));
        using var rejection = responses[0];
        using var approval = responses[1];

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        if (rejection.IsSuccessStatusCode)
        {
            await AssertErrorAsync(approval, HttpStatusCode.Conflict, "payment.order_not_pending");
            await AssertPersistedStateAsync(order.Id, OrderStatus.PaymentRejected, 0);
        }
        else
        {
            await AssertErrorAsync(rejection, HttpStatusCode.Conflict, "payment.order_not_pending");
            await AssertPersistedStateAsync(order.Id, OrderStatus.Confirmed, 2);
        }
    }

    [Fact]
    public async Task Should_Allow_Only_One_Transition_When_Rejection_Competes_With_Cancellation()
    {
        var order = await CreatePendingOrderAsync();
        var responses = await Task.WhenAll(RejectAsync(order.Id), CancelAsync(order.Id));
        using var rejection = responses[0];
        using var cancellation = responses[1];

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        if (rejection.IsSuccessStatusCode)
        {
            await AssertErrorAsync(cancellation, HttpStatusCode.Conflict, "order.cannot_cancel");
            await AssertPersistedStateAsync(order.Id, OrderStatus.PaymentRejected, 0);
        }
        else
        {
            await AssertErrorAsync(rejection, HttpStatusCode.Conflict, "payment.order_not_pending");
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
