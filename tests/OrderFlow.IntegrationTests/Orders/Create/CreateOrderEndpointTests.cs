using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Orders.Create;

[Collection<OrdersApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Orders")]
public sealed class CreateOrderEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Orders)
{
    [Fact]
    public async Task Should_Create_And_Persist_Order_With_Reserved_Inventory_When_Request_Is_Valid()
    {
        var product = await TestData.Product().WithPrice(100.00m).PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(5).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 2)), cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal($"/orders/{body.Id}", response.Headers.Location?.ToString());
        Assert.Equal("PendingPayment", body.Status);
        Assert.Equal(200.00m, body.TotalAmount);
        Assert.Single(body.Items);
        Assert.Equal(100.00m, body.Items.Single().UnitPrice);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var savedOrder = await dbContext.Orders.Include(order => order.OrderItems).SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        var inventory = await dbContext.InventoryItems.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(body.Id, savedOrder.Id);
        Assert.Single(savedOrder.OrderItems);
        Assert.Equal(2, inventory.ReservedQuantity);
        Assert.Equal(3, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Product_Is_Duplicated()
    {
        var productId = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((productId, 1), (productId, 2)), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Order_Has_No_Items()
    {
        var response = await Client.PostAsJsonAsync("/orders", CreateRequest(), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Item_Quantity_Is_Invalid()
    {
        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((Guid.NewGuid(), 0)), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((Guid.NewGuid(), 1)), cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("One or more products were not found.", body!.Detail);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Product_Is_Inactive()
    {
        var product = await TestData.Product().Inactive().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(5).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 1)), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Does_Not_Exist()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 1)), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_Roll_Back_All_Reservations_When_One_Item_Has_Insufficient_Inventory()
    {
        var firstProduct = await TestData.Product().WithName("Notebook").WithPrice(100.00m).PersistAsync(TestContext.Current.CancellationToken);
        var secondProduct = await TestData.Product().WithName("Mouse").WithPrice(50.00m).PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(firstProduct.Id).WithQuantity(5).PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(secondProduct.Id).WithQuantity(1).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((firstProduct.Id, 2), (secondProduct.Id, 2)), cancellationToken: TestContext.Current.CancellationToken);
        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("order.insufficient_inventory", body.Extensions["code"]?.ToString());

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventories = await dbContext.InventoryItems.AsNoTracking().ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(await dbContext.Orders.ToArrayAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.All(inventories, inventory => Assert.Equal(0, inventory.ReservedQuantity));
    }

    [Fact]
    public async Task Should_Create_Only_One_Order_When_Two_Requests_Compete_For_Last_Stock()
    {
        var product = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(1).PersistAsync(TestContext.Current.CancellationToken);
        var request = CreateRequest((product.Id, 1));

        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync("/orders", request, TestContext.Current.CancellationToken),
            Client.PostAsJsonAsync("/orders", request, TestContext.Current.CancellationToken));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        Assert.Equal(1, await dbContext.Orders.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, (await dbContext.InventoryItems.SingleAsync(cancellationToken: TestContext.Current.CancellationToken)).ReservedQuantity);
    }

    private static CreateOrderRequest CreateRequest(params (Guid ProductId, int Quantity)[] items)
    {
        return new CreateOrderRequest(
            items.Select(item => new CreateOrderItemRequest(item.ProductId, item.Quantity)).ToArray());
    }
}
