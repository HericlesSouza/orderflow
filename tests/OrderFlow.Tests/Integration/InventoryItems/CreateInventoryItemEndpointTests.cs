using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.InventoryItems;
using OrderFlow.Api.InventoryItems.Create;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.InventoryItems;

public sealed class CreateInventoryItemEndpointTests(ProductApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_Inventory_Item_When_Request_Is_Valid()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var response = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(product.Id, 10));

        var body = await response.Content.ReadFromJsonAsync<InventoryItemResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(product.Id, body.ProductId);
        Assert.Equal(10, body.Quantity);
        Assert.Equal(0, body.ReservedQuantity);
        Assert.Equal(10, body.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Persist_Inventory_Item_When_Request_Is_Valid()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var response = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(product.Id, 10));

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventoryItem = await dbContext.InventoryItems.SingleAsync();

        Assert.Equal(product.Id, inventoryItem.ProductId);
        Assert.Equal(10, inventoryItem.Quantity);
        Assert.Equal(0, inventoryItem.ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Request_Is_Invalid()
    {
        var response = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(Guid.Empty, -1));

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(CreateInventoryItemRequest.ProductId), body.Errors.Keys);
        Assert.Contains(nameof(CreateInventoryItemRequest.Quantity), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(Guid.NewGuid(), 0));

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Item_Already_Exists_For_Product()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var firstResponse = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(product.Id, 10));
        firstResponse.EnsureSuccessStatusCode();

        var secondResponse = await Client.PostAsJsonAsync("/inventory/items", new CreateInventoryItemRequest(product.Id, 5));
        var body = await secondResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Conflict detected.", body.Title);
        Assert.Equal("An inventory item already exists for this product.", body.Detail);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Using_Legacy_Inventory_Path()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var response = await Client.PostAsJsonAsync("/inventory-items", new CreateInventoryItemRequest(product.Id, 10));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
