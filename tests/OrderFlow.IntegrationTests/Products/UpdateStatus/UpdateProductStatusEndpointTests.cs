using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.UpdateStatus;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products.UpdateStatus;

[Collection<ProductsApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Products")]
public sealed class UpdateProductStatusEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Products)
{
    [Fact]
    public async Task Should_Deactivate_Product_When_Request_Is_Valid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/status", new UpdateProductStatusRequest(false), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdProduct.Id, body.Id);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task Should_Reactivate_Product_When_Request_Is_Valid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/status", new UpdateProductStatusRequest(false), cancellationToken: TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/status", new UpdateProductStatusRequest(true), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Should_Persist_Product_Status_When_Request_Is_Valid()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/status", new UpdateProductStatusRequest(false), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var product = await dbContext.Products.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Status_Is_Missing()
    {
        var createdProduct = await TestData.Product().PersistAsync(TestContext.Current.CancellationToken);

        var response = await Client.PatchAsJsonAsync($"/products/{createdProduct.Id}/status", new { }, cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(UpdateProductStatusRequest.IsActive), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PatchAsJsonAsync($"/products/{Guid.NewGuid()}/status", new UpdateProductStatusRequest(false), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.ReadProblemDetailsAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
    }
}
