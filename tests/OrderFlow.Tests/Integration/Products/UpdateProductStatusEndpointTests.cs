using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.UpdateStatus;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Products;

public sealed class UpdateProductStatusEndpointTests(ProductApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Deactivate_Product_When_Request_Is_Valid()
    {
        var createdProduct = await ProductSeeder.CreateProductAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/products/{createdProduct.Id}/status",
            new UpdateProductStatusRequest(false));

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal(createdProduct.Id, body.Id);
        Assert.False(body.IsActive);
    }

    [Fact]
    public async Task Should_Reactivate_Product_When_Request_Is_Valid()
    {
        var createdProduct = await ProductSeeder.CreateProductAsync();

        await Client.PatchAsJsonAsync(
            $"/products/{createdProduct.Id}/status",
            new UpdateProductStatusRequest(false));

        var response = await Client.PatchAsJsonAsync(
            $"/products/{createdProduct.Id}/status",
            new UpdateProductStatusRequest(true));

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Should_Persist_Product_Status_When_Request_Is_Valid()
    {
        var createdProduct = await ProductSeeder.CreateProductAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/products/{createdProduct.Id}/status",
            new UpdateProductStatusRequest(false));

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        var product = await dbContext.Products.SingleAsync();

        Assert.False(product.IsActive);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Status_Is_Missing()
    {
        var createdProduct = await ProductSeeder.CreateProductAsync();

        var response = await Client.PatchAsJsonAsync(
            $"/products/{createdProduct.Id}/status",
            new { });

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(UpdateProductStatusRequest.IsActive), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var response = await Client.PatchAsJsonAsync(
            $"/products/{Guid.NewGuid()}/status",
            new UpdateProductStatusRequest(false));

        var body = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Resource not found.", body.Title);
        Assert.Equal("Product not found.", body.Detail);
    }
}
