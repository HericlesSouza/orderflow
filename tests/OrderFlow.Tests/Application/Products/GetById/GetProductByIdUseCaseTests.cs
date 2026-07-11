using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products.GetById;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class GetProductByIdUseCaseTests
{
    [Fact]
    public async Task Should_Return_Product_When_Product_Exists()
    {
        var product = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var useCase = new GetProductByIdUseCase(repositoryMock.Object);

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
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var useCase = new GetProductByIdUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.not_found", result.Error.Code);
        Assert.Equal(AppErrorType.NotFound, result.Error.Type);
        Assert.Equal("Product not found.", result.Error.Message);
    }
}
