using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products.GetById;
using OrderFlow.Domain.Entities;
using OrderFlow.Tests.Common.Fakes;

namespace OrderFlow.Tests;

public sealed class GetProductByIdUseCaseTests
{
    [Fact]
    public async Task Should_Return_Product_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repository = new FakeProductRepository([product]);
        var useCase = new GetProductByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(product.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(product.Id, result.Value!.Id);
        Assert.Equal(product.Name, result.Value.Name);
        Assert.Equal(product.Description, result.Value.Description);
        Assert.Equal(product.Price, result.Value.Price);
        Assert.Equal(product.IsActive, result.Value.IsActive);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var repository = new FakeProductRepository();
        var useCase = new GetProductByIdUseCase(repository);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
    }
}
