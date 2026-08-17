using Moq;
using OrderFlow.Application.Products.GetAll;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;
using OrderFlow.UnitTests.Common.Builders;

namespace OrderFlow.UnitTests.Application.Products.GetAll;

[Trait("Layer", "Application")]
[Trait("Feature", "Products")]
public sealed class GetProductsUseCaseTests
{
    [Fact]
    public async Task Should_Return_Empty_List_When_There_Are_No_Products()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        var useCase = new GetProductsUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_Return_Products_Mapped_To_Output_When_Repository_Returns_Products()
    {
        var firstProduct = new ProductBuilder().WithName("Notebook").WithDescription("Gaming notebook").WithPrice(3500.00m).Build();
        var secondProduct = new ProductBuilder().WithName("Mouse").WithDescription("Wireless mouse").WithPrice(150.00m).Build();
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([secondProduct, firstProduct]);

        var useCase = new GetProductsUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(secondProduct.Id, result[0].Id);
        Assert.Equal("Mouse", result[0].Name);
        Assert.Equal(firstProduct.Id, result[1].Id);
        Assert.Equal("Notebook", result[1].Name);
    }
}
