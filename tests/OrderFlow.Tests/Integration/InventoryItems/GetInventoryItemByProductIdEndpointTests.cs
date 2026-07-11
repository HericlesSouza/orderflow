using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.InventoryItems;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.InventoryItems;

public sealed class GetInventoryItemByProductIdEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Inventory_Item_When_Inventory_Exists_For_Product()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 10);
        var persistedInventoryItem = await InventoryItemSeeder.GetInventoryItemByProductIdAsync(product.Id);

        var response = await Client.GetAsync($"/inventory/items/{product.Id}");
        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.NotNull(persistedInventoryItem);
        Assert.Equal(persistedInventoryItem!.Id, body.Id);
        Assert.Equal(product.Id, body.ProductId);
        Assert.Equal(persistedInventoryItem.Quantity, body.Quantity);
        Assert.Equal(persistedInventoryItem.ReservedQuantity, body.ReservedQuantity);
        Assert.Equal(persistedInventoryItem.AvailableQuantity, body.AvailableQuantity);
        Assert.Equal(persistedInventoryItem.CreatedAt, body.CreatedAt);
        Assert.Equal(persistedInventoryItem.UpdatedAt, body.UpdatedAt);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Inventory_Item_Does_Not_Exist_For_Product()
    {
        var productId = Guid.NewGuid();

        var response = await Client.GetAsync($"/inventory/items/{productId}");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("No inventory item found for the specified product.", body.Detail);
    }
}
