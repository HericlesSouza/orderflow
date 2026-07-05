using OrderFlow.Application.Products.UpdatePrice;
using OrderFlow.Domain.Entities;
using OrderFlow.Tests.Common.Fakes;

namespace OrderFlow.Tests;

public sealed class UpdateProductPriceUseCaseTests
{
    [Fact]
    public async Task Should_Update_Product_Price_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repository = new FakeProductRepository([product]);
        var useCase = new UpdateProductPriceUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(product.Id, 4200.00m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(4200.00m, result.Value!.Price);
        Assert.Equal(1, repository.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var repository = new FakeProductRepository();
        var useCase = new UpdateProductPriceUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(Guid.NewGuid(), 4200.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found.", result.Error);
        Assert.Equal(0, repository.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Price_Is_Invalid()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repository = new FakeProductRepository([product]);
        var useCase = new UpdateProductPriceUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(product.Id, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product price must be greater than zero.", result.Error);
        Assert.Equal(0, repository.UpdateAsyncCallCount);
    }
}
