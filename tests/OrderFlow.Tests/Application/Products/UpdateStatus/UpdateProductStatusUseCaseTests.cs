using OrderFlow.Application.Products.UpdateStatus;
using OrderFlow.Domain.Entities;
using OrderFlow.Tests.Common.Fakes;

namespace OrderFlow.Tests;

public sealed class UpdateProductStatusUseCaseTests
{
    [Fact]
    public async Task Should_Deactivate_Product_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repository = new FakeProductRepository([product]);
        var useCase = new UpdateProductStatusUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(product.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value!.IsActive);
        Assert.Equal(1, repository.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var repository = new FakeProductRepository();
        var useCase = new UpdateProductStatusUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(Guid.NewGuid(), false), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Product not found.", result.Error);
        Assert.Equal(0, repository.UpdateAsyncCallCount);
    }

    [Fact]
    public async Task Should_Activate_Product_When_Product_Is_Currently_Inactive()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        product.SetStatus(false);
        var repository = new FakeProductRepository([product]);
        var useCase = new UpdateProductStatusUseCase(repository);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(product.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.IsActive);
        Assert.Equal(1, repository.UpdateAsyncCallCount);
    }
}
