using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Products;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products.GetById;

[Collection<ProductsApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Products")]
public sealed class GetProductByIdEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Products)
{
    [Fact]
    public async Task Should_Return_Product_When_Product_Exists()
    {
        var createdProduct = await TestData.Product().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).PersistAsync(TestContext.Current.CancellationToken);

        var getResponse = await Client.GetAsync($"/products/{createdProduct.Id}", TestContext.Current.CancellationToken);
        var body = await getResponse.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdProduct.Id, body.Id);
        Assert.Equal(createdProduct.Name, body.Name);
        Assert.Equal(createdProduct.Description, body.Description);
        Assert.Equal(createdProduct.Price, body.Price);
        Assert.Equal(createdProduct.IsActive, body.IsActive);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.GetAsync($"/products/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
        Assert.Equal("product.not_found", body.Extensions["code"]?.ToString());
    }
}
