using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Products.Create;
using OrderFlow.Api.Products;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.Tests.Integration.Support;

namespace OrderFlow.Tests.Integration.Products;

public sealed class CreateProductEndpointTests(OrderFlowApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Should_Create_Product_When_Request_Is_Valid()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "Notebook",
            "Gaming notebook",
            3500.00m));

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(body);
        Assert.Equal("Notebook", body.Name);
        Assert.Equal("Gaming notebook", body.Description);
        Assert.Equal(3500.00m, body.Price);
        Assert.True(body.IsActive);
    }

    [Fact]
    public async Task Should_Persist_Product_When_Request_Is_Valid()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "Notebook",
            "Gaming notebook",
            3500.00m));

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        var product = await dbContext.Products.SingleAsync();

        Assert.Equal("Notebook", product.Name);
        Assert.Equal("Gaming notebook", product.Description);
        Assert.Equal(3500.00m, product.Price);
        Assert.True(product.IsActive);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Name_Is_Empty()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "",
            "Gaming notebook",
            3500.00m));

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(CreateProductRequest.Name), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Description_Is_Empty()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "Notebook",
            "",
            3500.00m));

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(CreateProductRequest.Description), body.Errors.Keys);
    }

    [Fact]
    public async Task Should_Return_BadRequest_When_Price_Is_Zero()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "Notebook",
            "Gaming notebook",
            0));

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(nameof(CreateProductRequest.Price), body.Errors.Keys);
    }
}
