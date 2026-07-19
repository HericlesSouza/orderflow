using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Orders;

public sealed class OrderEndpointsTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_And_Persist_Order_With_Reserved_Inventory()
    {
        var product = await ProductSeeder.CreateProductAsync(price: 100.00m);
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);

        var response = await Client.PostAsJsonAsync(
            "/orders",
            CreateRequest((product.Id, 2)));
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal($"/orders/{body.Id}", response.Headers.Location?.ToString());
        Assert.Equal("PendingPayment", body.Status);
        Assert.Equal(200.00m, body.TotalAmount);
        Assert.Single(body.Items);
        Assert.Equal(100.00m, body.Items.Single().UnitPrice);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var savedOrder = await dbContext.Orders.Include(order => order.OrderItems).SingleAsync();
        var inventory = await dbContext.InventoryItems.SingleAsync();

        Assert.Equal(body.Id, savedOrder.Id);
        Assert.Single(savedOrder.OrderItems);
        Assert.Equal(2, inventory.ReservedQuantity);
        Assert.Equal(3, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task Should_Get_Order_With_Items_When_Order_Exists()
    {
        var product = await ProductSeeder.CreateProductAsync(price: 75.00m);
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);
        var creationResponse = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 3)));
        var createdOrder = await creationResponse.Content.ReadFromJsonAsync<OrderResponse>();

        var response = await Client.GetAsync($"/orders/{createdOrder!.Id}");
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdOrder.Id, body.Id);
        Assert.Equal(225.00m, body.TotalAmount);
        Assert.Single(body.Items);
    }

    [Fact]
    public async Task Should_Keep_Original_Unit_Price_When_Product_Price_Changes()
    {
        var product = await ProductSeeder.CreateProductAsync(price: 100.00m);
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);
        var creationResponse = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 1)));
        var createdOrder = await creationResponse.Content.ReadFromJsonAsync<OrderResponse>();

        product.ChangePrice(250.00m);
        await using (var scope = Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
            dbContext.Products.Update(product);
            await dbContext.SaveChangesAsync();
        }

        var response = await Client.GetFromJsonAsync<OrderResponse>($"/orders/{createdOrder!.Id}");

        Assert.NotNull(response);
        Assert.Equal(100.00m, response.Items.Single().UnitPrice);
        Assert.Equal(100.00m, response.TotalAmount);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Product_Is_Duplicated()
    {
        var productId = Guid.NewGuid();

        var response = await Client.PostAsJsonAsync(
            "/orders",
            CreateRequest((productId, 1), (productId, 2)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Order_Has_No_Items()
    {
        var response = await Client.PostAsJsonAsync("/orders", CreateRequest());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Item_Quantity_Is_Invalid()
    {
        var response = await Client.PostAsJsonAsync(
            "/orders",
            CreateRequest((Guid.NewGuid(), 0)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PostAsJsonAsync(
            "/orders",
            CreateRequest((Guid.NewGuid(), 1)));
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("One or more products were not found.", body!.Detail);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Product_Is_Inactive()
    {
        var product = await ProductSeeder.CreateProductAsync(isActive: false);
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 5);

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_Return_Conflict_When_Inventory_Does_Not_Exist()
    {
        var product = await ProductSeeder.CreateProductAsync();

        var response = await Client.PostAsJsonAsync("/orders", CreateRequest((product.Id, 1)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Should_Roll_Back_All_Reservations_When_One_Item_Has_Insufficient_Inventory()
    {
        var firstProduct = await ProductSeeder.CreateProductAsync("Notebook", price: 100.00m);
        var secondProduct = await ProductSeeder.CreateProductAsync("Mouse", price: 50.00m);
        await InventoryItemSeeder.CreateInventoryItemAsync(firstProduct.Id, 5);
        await InventoryItemSeeder.CreateInventoryItemAsync(secondProduct.Id, 1);

        var response = await Client.PostAsJsonAsync(
            "/orders",
            CreateRequest((firstProduct.Id, 2), (secondProduct.Id, 2)));
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("order.insufficient_inventory", body.Extensions["code"]?.ToString());

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var inventories = await dbContext.InventoryItems.AsNoTracking().ToArrayAsync();

        Assert.Empty(await dbContext.Orders.ToArrayAsync());
        Assert.All(inventories, inventory => Assert.Equal(0, inventory.ReservedQuantity));
    }

    [Fact]
    public async Task Should_Create_Only_One_Order_When_Two_Requests_Compete_For_Last_Stock()
    {
        var product = await ProductSeeder.CreateProductAsync();
        await InventoryItemSeeder.CreateInventoryItemAsync(product.Id, 1);
        var request = CreateRequest((product.Id, 1));

        var responses = await Task.WhenAll(
            Client.PostAsJsonAsync("/orders", request),
            Client.PostAsJsonAsync("/orders", request));

        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        Assert.Equal(1, await dbContext.Orders.CountAsync());
        Assert.Equal(1, (await dbContext.InventoryItems.SingleAsync()).ReservedQuantity);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var response = await Client.GetAsync($"/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static CreateOrderRequest CreateRequest(params (Guid ProductId, int Quantity)[] items)
    {
        return new CreateOrderRequest(
            items.Select(item => new CreateOrderItemRequest(item.ProductId, item.Quantity)).ToArray());
    }
}
