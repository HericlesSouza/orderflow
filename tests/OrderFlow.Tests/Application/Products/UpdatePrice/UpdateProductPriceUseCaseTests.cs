using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products.UpdatePrice;
using OrderFlow.Application.Products;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class UpdateProductPriceUseCaseTests
{
    [Fact]
    public async Task Should_Update_Product_Price_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateProductPriceUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(product.Id, 4200.00m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(4200.00m, result.Value!.Price);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Return_NotFound_When_Product_Does_Not_Exist()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var useCase = new UpdateProductPriceUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(Guid.NewGuid(), 4200.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Price_Is_Invalid()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var useCase = new UpdateProductPriceUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductPriceInput(product.Id, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("Product price must be greater than zero.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
