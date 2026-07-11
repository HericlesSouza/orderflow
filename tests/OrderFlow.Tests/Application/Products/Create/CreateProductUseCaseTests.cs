using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Products;
using OrderFlow.Domain.Entities;
using OrderFlow.Application.Repositories;

namespace OrderFlow.Tests;

public sealed class CreateProductUseCaseTests
{
    [Fact]
    public async Task Should_Create_Product_When_Input_Is_Valid()
    {
        Product? savedProduct = null;
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => savedProduct = product)
            .Returns(Task.CompletedTask);

        var useCase = new CreateProductUseCase(repositoryMock.Object);
        var input = new CreateProductInput("Notebook", "Gaming notebook", 3500.00m);

        var result = await useCase.ExecuteAsync(input, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(savedProduct);
        Assert.Equal("Notebook", result.Value!.Name);
        Assert.Equal("Gaming notebook", result.Value.Description);
        Assert.Equal(3500.00m, result.Value.Price);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Name_Is_Invalid()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateProductInput("", "Gaming notebook", 3500.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("Product name cannot be empty.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Description_Is_Invalid()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "", 3500.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("Product description cannot be empty.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Price_Is_Invalid()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal("Product price must be greater than zero.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Name_Exceeds_Max_Length()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repositoryMock.Object);
        var longName = new string('N', Product.NameMaxLength + 1);

        var result = await useCase.ExecuteAsync(new CreateProductInput(longName, "Gaming notebook", 3500.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal($"Product name cannot be longer than {Product.NameMaxLength} characters.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Description_Exceeds_Max_Length()
    {
        var repositoryMock = new Mock<IProductRepository>();
        var useCase = new CreateProductUseCase(repositoryMock.Object);
        var longDescription = new string('D', Product.DescriptionMaxLength + 1);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", longDescription, 3500.00m), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal($"Product description cannot be longer than {Product.DescriptionMaxLength} characters.", result.Error.Message);
        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Call_Repository_Once_When_Input_Is_Valid()
    {
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var useCase = new CreateProductUseCase(repositoryMock.Object);

        await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m), CancellationToken.None);

        repositoryMock.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Map_Product_To_Output_When_Creation_Succeeds()
    {
        Product? savedProduct = null;
        var repositoryMock = new Mock<IProductRepository>();
        repositoryMock
            .Setup(repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Callback<Product, CancellationToken>((product, _) => savedProduct = product)
            .Returns(Task.CompletedTask);

        var useCase = new CreateProductUseCase(repositoryMock.Object);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m), CancellationToken.None);

        Assert.NotNull(savedProduct);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(savedProduct!.Id, result.Value!.Id);
        Assert.Equal(savedProduct.Name, result.Value.Name);
        Assert.Equal(savedProduct.Description, result.Value.Description);
        Assert.Equal(savedProduct.Price, result.Value.Price);
        Assert.Equal(savedProduct.IsActive, result.Value.IsActive);
        Assert.Equal(savedProduct.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(savedProduct.UpdatedAt, result.Value.UpdatedAt);
    }
}
