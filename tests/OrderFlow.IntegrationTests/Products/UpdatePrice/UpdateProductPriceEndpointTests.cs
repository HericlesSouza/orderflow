using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.UpdatePrice;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products.UpdatePrice;

[Collection<ProductsApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Products")]
public sealed class UpdateProductPriceEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Products)
{
    [Fact]
    public async Task Should_Update_Product_Price_When_Request_Is_Valid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/price", new UpdateProductPriceRequest(4200.00m), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdProduct.Id, body.Id);
        Assert.Equal(4200.00m, body.Price);
    }

    [Fact]
    public async Task Should_Persist_Product_Price_When_Request_Is_Valid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/price", new UpdateProductPriceRequest(4200.00m), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var product = await dbContext.Products.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(4200.00m, product.Price);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Price_Is_Invalid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/price", new UpdateProductPriceRequest(0), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(UpdateProductPriceRequest.Price), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PatchAsJsonAsync($"/products/{Guid.NewGuid()}/price", new UpdateProductPriceRequest(4200.00m), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
    }
}
