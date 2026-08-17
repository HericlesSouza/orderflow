using Moq;
using OrderFlow.Application.Common.Errors;
using OrderFlow.Application.Products;
using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Application.Products.Create;

[Trait("Layer", "Application")]
[Trait("Feature", "Products")]
public sealed class CreateProductUseCaseTests
{
    [Fact]
    public async Task Should_Create_Product_When_Input_Is_Valid()
    {
        var fixture = new CreateProductUseCaseFixture();
        var useCase = fixture.CreateSut();
        var input = new CreateProductInput("Notebook", "Gaming notebook", 3500.00m);

        var result = await useCase.ExecuteAsync(input, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotNull(fixture.SavedProduct);
        Assert.Equal("Notebook", result.Value!.Name);
        Assert.Equal("Gaming notebook", result.Value.Description);
        Assert.Equal(3500.00m, result.Value.Price);
        Assert.True(result.Value.IsActive);
    }

    public static TheoryData<CreateProductInput, string> InvalidInputs => new()
    {
        { new CreateProductInput("", "Gaming notebook", 3500.00m), "Product name cannot be empty." },
        { new CreateProductInput("Notebook", "", 3500.00m), "Product description cannot be empty." },
        { new CreateProductInput("Notebook", "Gaming notebook", 0), "Product price must be greater than zero." },
        {
            new CreateProductInput(new string('N', Product.NameMaxLength + 1), "Gaming notebook", 3500.00m),
            $"Product name cannot be longer than {Product.NameMaxLength} characters."
        },
        {
            new CreateProductInput("Notebook", new string('D', Product.DescriptionMaxLength + 1), 3500.00m),
            $"Product description cannot be longer than {Product.DescriptionMaxLength} characters."
        }
    };

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public async Task Should_Return_Validation_When_Input_Is_Invalid(
        CreateProductInput input,
        string expectedMessage)
    {
        var fixture = new CreateProductUseCaseFixture();
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(input, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Equal("product.invalid", result.Error.Code);
        Assert.Equal(AppErrorType.Validation, result.Error.Type);
        Assert.Equal(expectedMessage, result.Error.Message);
        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Should_Call_Repository_Once_When_Input_Is_Valid()
    {
        var fixture = new CreateProductUseCaseFixture();
        var useCase = fixture.CreateSut();

        await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m), CancellationToken.None);

        fixture.Repository.Verify(
            repository => repository.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Should_Map_Product_To_Output_When_Creation_Succeeds()
    {
        var fixture = new CreateProductUseCaseFixture();
        var useCase = fixture.CreateSut();

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m), CancellationToken.None);

        Assert.NotNull(fixture.SavedProduct);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(fixture.SavedProduct!.Id, result.Value!.Id);
        Assert.Equal(fixture.SavedProduct.Name, result.Value.Name);
        Assert.Equal(fixture.SavedProduct.Description, result.Value.Description);
        Assert.Equal(fixture.SavedProduct.Price, result.Value.Price);
        Assert.Equal(fixture.SavedProduct.IsActive, result.Value.IsActive);
        Assert.Equal(fixture.SavedProduct.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(fixture.SavedProduct.UpdatedAt, result.Value.UpdatedAt);
    }

    private sealed class CreateProductUseCaseFixture
    {
        public CreateProductUseCaseFixture()
        {
            Repository
                .Setup(repository => repository.AddAsync(
                    It.IsAny<Product>(),
                    It.IsAny<CancellationToken>()))
                .Callback<Product, CancellationToken>((product, _) => SavedProduct = product)
                .Returns(Task.CompletedTask);
        }

        public Mock<IProductRepository> Repository { get; } = new();
        public Product? SavedProduct { get; private set; }

        public CreateProductUseCase CreateSut()
        {
            return new CreateProductUseCase(Repository.Object);
        }
    }
}
