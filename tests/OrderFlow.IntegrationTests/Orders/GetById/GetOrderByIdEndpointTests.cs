using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Orders;
using OrderFlow.Api.Orders.Create;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Orders.GetById;

[Collection<OrdersApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Orders")]
public sealed class GetOrderByIdEndpointTests(
    OrderFlowIntegrationSuiteFixture environment)
    : FeatureApiTest(environment, IntegrationFeatures.Orders)
{
    [Fact]
    public async Task Should_Get_Order_With_Items_When_Order_Exists()
    {
        var product = await TestData.Product().WithPrice(75.00m).PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(5).PersistAsync(TestContext.Current.CancellationToken);
        var createdOrder = await CreateOrderAsync(product.Id, 3);

        var response = await Client.GetAsync(
            $"/orders/{createdOrder.Id}",
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<OrderResponse>(
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdOrder.Id, body.Id);
        Assert.Equal(225.00m, body.TotalAmount);
        Assert.Single(body.Items);
    }

    [Fact]
    public async Task Should_Keep_Original_Unit_Price_When_Product_Price_Changes()
    {
        var product = await TestData.Product().WithPrice(100.00m).PersistAsync(TestContext.Current.CancellationToken);
        await TestData.InventoryItem().ForProduct(product.Id).WithQuantity(5).PersistAsync(TestContext.Current.CancellationToken);
        var createdOrder = await CreateOrderAsync(product.Id, 1);

        product.ChangePrice(250.00m);
        await TestData.UpdateProductAsync(product, TestContext.Current.CancellationToken);

        var response = await Client.GetFromJsonAsync<OrderResponse>(
            $"/orders/{createdOrder.Id}",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(response);
        Assert.Equal(100.00m, response.Items.Single().UnitPrice);
        Assert.Equal(100.00m, response.TotalAmount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Order_Does_Not_Exist()
    {
        var response = await Client.GetAsync(
            $"/orders/{Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<OrderResponse> CreateOrderAsync(Guid productId, int quantity)
    {
        var request = new CreateOrderRequest(
            [new CreateOrderItemRequest(productId, quantity)]);

        var response = await Client.PostAsJsonAsync(
            "/orders",
            request,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderResponse>(
            cancellationToken: TestContext.Current.CancellationToken))!;
    }
}
