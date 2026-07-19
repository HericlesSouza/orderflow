using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Domain.Enums;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Orders;

public sealed class CancelOrderEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Cancel_Order_And_Release_Reserved_Inventory()
    {
        var product = await ProductSeeder.CreateProductAsync(price: 100.00m);
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);
        var order = await CreateOrderAsync(product.Id, 2);

        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", content: null);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(order.Id, body.Id);
        Assert.Equal("Cancelled", body.Status);
        Assert.NotNull(body.CancelledAt);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var savedOrder = await dbContext.Orders.SingleAsync();
        var inventory = await dbContext.InventoryItems.SingleAsync();

        Assert.Equal(OrderStatus.Cancelled, savedOrder.Status);
        Assert.NotNull(savedOrder.CancelledAt);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(5, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var response = await Client.PostAsync($"/orders/{Guid.NewGuid()}/cancel", content: null);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("Order not found.", body!.Detail);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Order_Is_Already_Cancelled_Without_Releasing_Inventory_Twice()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);
        var order = await CreateOrderAsync(product.Id, 2);
        await Client.PostAsync($"/orders/{order.Id}/cancel", content: null);

        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", content: null);
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Order is already cancelled.", body.Detail);
        Assert.True(body.Extensions.TryGetValue("code", out var errorCode));
        Assert.Equal("order.cannot_cancel", errorCode?.ToString());

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventory = await dbContext.InventoryItems.SingleAsync();

        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(5, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Roll_Back_Cancellation_When_Reserved_Inventory_Is_Inconsistent()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);
        var order = await CreateOrderAsync(product.Id, 2);

        await using (var scope = Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE inventory_items
                SET reserved_quantity = 1
                WHERE product_id = {product.Id}
                """);
        }

        var response = await Client.PostAsync($"/orders/{order.Id}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        await using var verificationScope = Services.CreateAsyncScope();
        var verificationDbContext = verificationScope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var savedOrder = await verificationDbContext.Orders.SingleAsync();
        var inventory = await verificationDbContext.InventoryItems.SingleAsync();

        Assert.Equal(OrderStatus.PendingPayment, savedOrder.Status);
        Assert.Null(savedOrder.CancelledAt);
        Assert.Equal(1, inventory.ReservedQuantity);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid productId, int quantity)
    {
        var response = await Client.PostAsJsonAsync(
            "/orders",
            new CreateOrderRequest([new CreateOrderItemRequest(productId, quantity)]));

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>())!;
    }
}
