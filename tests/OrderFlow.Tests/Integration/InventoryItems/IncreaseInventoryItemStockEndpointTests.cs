using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.InventoryItems;
using OrderFlow.Api.InventoryItems.Increase;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.InventoryItems;

public sealed class IncreaseInventoryItemStockEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Updated_Inventory_Item_When_Request_Is_Valid()
    {
        var product = await ProductSeeder.CreateProductAsync();
        var inventoryItem = await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 10);

        var response = await Client.PostAsJsonAsync(
            $"/inventory/items/{product.Id}/increase",
            new IncreaseInventoryItemStockRequest(5));

        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(inventoryItem.Id, body.Id);
        Assert.Equal(product.Id, body.ProductId);
        Assert.Equal(15, body.Quantity);
        Assert.Equal(0, body.ReservedQuantity);
        Assert.Equal(15, body.AvailableQuantity);
        Assert.Equal(inventoryItem.CreatedAt, body.CreatedAt, TimeSpan.FromMilliseconds(1));
        Assert.True(body.UpdatedAt >= inventoryItem.UpdatedAt);
    }

    [Fact]
    public async Task Should_Persist_Increased_Inventory_Stock_When_Request_Is_Valid()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 10);

        var response = await Client.PostAsJsonAsync(
            $"/inventory/items/{product.Id}/increase",
            new IncreaseInventoryItemStockRequest(5));

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventoryItem = await dbContext.InventoryItems.SingleAsync();

        Assert.Equal(15, inventoryItem.Quantity);
        Assert.Equal(0, inventoryItem.ReservedQuantity);
        Assert.Equal(15, inventoryItem.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Quantity_Is_Invalid()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 10);

        var response = await Client.PostAsJsonAsync(
            $"/inventory/items/{product.Id}/increase",
            new IncreaseInventoryItemStockRequest(0));

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(IncreaseInventoryItemStockRequest.Quantity), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Inventory_Item_Does_Not_Exist()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var response = await Client.PostAsJsonAsync(
            $"/inventory/items/{product.Id}/increase",
            new IncreaseInventoryItemStockRequest(5));

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("No inventory item found for the specified product.", body.Detail);
    }
}
