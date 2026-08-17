using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Api.Products;
using OrderFlow.Api.Products.Create;
using OrderFlow.Infrastructure.Persistence;
using OrderFlow.IntegrationTests.Infrastructure;

namespace OrderFlow.IntegrationTests.Products.Create;

[Collection<ProductsApiCollection>]
[Trait("Layer", "Api")]
[Trait("Feature", "Products")]
public sealed class CreateProductEndpointTests(OrderFlowIntegrationSuiteFixture environment) : FeatureApiTest(environment, IntegrationFeatures.Products)
{
    [Fact]
    public async Task Should_Create_Product_When_Request_Is_Valid()
    {
        var response = await Client.PostAsJsonAsync("/products", new CreateProductRequest(
            "Notebook",
            "Gaming notebook",
            3500.00m), cancellationToken: TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ProductResponse>(cancellationToken: TestContext.Current.CancellationToken);

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
            3500.00m), cancellationToken: TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();

        var product = await dbContext.Products.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Notebook", product.Name);
        Assert.Equal("Gaming notebook", product.Description);
        Assert.Equal(3500.00m, product.Price);
        Assert.True(product.IsActive);
    }

    public static IEnumerable<TheoryDataRow<CreateProductRequest, string>> InvalidRequests()
    {
        yield return new TheoryDataRow<CreateProductRequest, string>(
            new CreateProductRequest("", "Gaming notebook", 3500.00m),
            nameof(CreateProductRequest.Name))
        {
            TestDisplayName = "Empty product name is rejected"
        };

        yield return new TheoryDataRow<CreateProductRequest, string>(
            new CreateProductRequest("Notebook", "", 3500.00m),
            nameof(CreateProductRequest.Description))
        {
            TestDisplayName = "Empty product description is rejected"
        };

        yield return new TheoryDataRow<CreateProductRequest, string>(
            new CreateProductRequest("Notebook", "Gaming notebook", 0),
            nameof(CreateProductRequest.Price))
        {
            TestDisplayName = "Non-positive product price is rejected"
        };
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Should_Return_BadRequest_When_Request_Is_Invalid(
        CreateProductRequest request,
        string expectedField)
    {
        var response = await Client.PostAsJsonAsync(
            "/products",
            request,
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(expectedField, body.Errors.Keys);
    }
}
