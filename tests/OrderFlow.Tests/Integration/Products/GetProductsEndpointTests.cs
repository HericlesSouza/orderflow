using System.Net;
using System.Net.Http.Json;
using OrderFlow.Api.Products;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Products;

public sealed class GetProductsEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Empty_List_When_There_Are_No_Products()
    {
        var response = await Client.GetAsync("/products");
        var body = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Empty(body);
    }

    [Fact]
    public async Task Should_Return_All_Products_Ordered_By_Most_Recent_Creation_Date()
    {
        var firstProduct = await ProductSeeder.CreateProductAsync("Notebook", "Gaming notebook", 3500.00m);
        await Task.Delay(20);
        var secondProduct = await ProductSeeder.CreateProductAsync("Mouse", "Wireless mouse", 150.00m);
        await Task.Delay(20);
        var thirdProduct = await ProductSeeder.CreateProductAsync("Keyboard", "Mechanical keyboard", 400.00m);

        var response = await Client.GetAsync("/products");
        var body = await response.Content.ReadFromJsonAsync<ProductResponse[]>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(3, body.Length);
        Assert.Equal(thirdProduct.Id, body[0].Id);
        Assert.Equal(secondProduct.Id, body[1].Id);
        Assert.Equal(firstProduct.Id, body[2].Id);
    }
}
