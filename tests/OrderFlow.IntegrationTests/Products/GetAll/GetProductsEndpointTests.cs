using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products.GetAll;

[Collection<ProductsApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Products")]
public sealed class GetProductsEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Products)
{
    [Fact]
    public async Task Should_Return_Empty_List_When_There_Are_No_Products()
    {
        var response = await Client.GetAsync("/products", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse[]>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Should_Return_All_Products_Ordered_By_Most_Recent_Creation_Date_When_Products_Exist()
    {
        var firstProduct = await TestData.Product().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).PersistAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);
        var secondProduct = await TestData.Product().WithName("Mouse").WithDescription("Wireless mouse").WithPrice(150.00m).PersistAsync(TestContext.Current.CancellationToken);
        await Task.Delay(20, TestContext.Current.CancellationToken);
        var thirdProduct = await TestData.Product().WithName("Keyboard").WithDescription("Mechanical keyboard").WithPrice(400.00m).PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.GetAsync("/products", TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<ProductResponse[]>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(3, body.Length);
        Assert.Equal(thirdProduct.Id, body[0].Id);
        Assert.Equal(secondProduct.Id, body[1].Id);
        Assert.Equal(firstProduct.Id, body[2].Id);
    }
}
