using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Api.Products;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Products;

public sealed class GetProductByIdEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Return_Product_When_Product_Exists()
    {
        var createdProduct = await ProductSeeder.CreateProductAsync(
            "Notebook",
            "Gaming notebook",
            3500.00m);

        var getResponse = await Client.GetAsync($"/products/{createdProduct.Id}");
        var body = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();

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
        var response = await Client.GetAsync($"/products/{Guid.NewGuid()}");
        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
    }
}
