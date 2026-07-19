using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products;
using OrderFlow.Application.Products.UpdateStatus;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class UpdateProductStatusUseCaseTests
{
    [Fact]
    public async Task Should_Deactivate_Product_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateProductStatusUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(product.Id, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.False(result.Value!.IsActive);
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

        var useCase = new UpdateProductStatusUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(Guid.NewGuid(), false), CancellationToken.None);

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
    public async Task Should_Activate_Product_When_Product_Is_Currently_Inactive()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        product.SetStatus(false);
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        repositoryMock
            .Setup(repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new UpdateProductStatusUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new UpdateProductStatusInput(product.Id, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.True(result.Value!.IsActive);
        repositoryMock.Verify(
            repository => repository.UpdateAsync(product, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
