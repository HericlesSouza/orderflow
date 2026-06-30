using OrderFlow.Application.Products.Create;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class CreateProductUseCaseTests
{
    [Fact]
    public async Task Should_Create_Product_When_Input_Is_Valid()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);
        var input = new CreateProductInput("Notebook", "Gaming notebook", 3500.00m);

        var result = await useCase.ExecuteAsync(input);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(repository.AddedProducts);
        Assert.Equal("Notebook", result.Value!.Name);
        Assert.Equal("Gaming notebook", result.Value.Description);
        Assert.Equal(3500.00m, result.Value.Price);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Name_Is_Invalid()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);

        var result = await useCase.ExecuteAsync(new CreateProductInput("", "Gaming notebook", 3500.00m));

        Assert.True(result.IsFailure);
        Assert.Equal("Product name cannot be empty.", result.Error);
        Assert.Empty(repository.AddedProducts);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Description_Is_Invalid()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "", 3500.00m));

        Assert.True(result.IsFailure);
        Assert.Equal("Product description cannot be empty.", result.Error);
        Assert.Empty(repository.AddedProducts);
    }

    [Fact]
    public async Task Should_Return_Failure_When_Price_Is_Invalid()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 0));

        Assert.True(result.IsFailure);
        Assert.Equal("Product price must be greater than zero.", result.Error);
        Assert.Empty(repository.AddedProducts);
    }

    [Fact]
    public async Task Should_Call_Repository_Once_When_Input_Is_Valid()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);

        await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m));

        Assert.Equal(1, repository.AddAsyncCallCount);
    }

    [Fact]
    public async Task Should_Map_Product_To_Output_When_Creation_Succeeds()
    {
        var repository = new FakeProductRepository();
        var useCase = new CreateProductUseCase(repository);

        var result = await useCase.ExecuteAsync(new CreateProductInput("Notebook", "Gaming notebook", 3500.00m));

        var savedProduct = Assert.Single(repository.AddedProducts);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(savedProduct.Id, result.Value!.Id);
        Assert.Equal(savedProduct.Name, result.Value.Name);
        Assert.Equal(savedProduct.Description, result.Value.Description);
        Assert.Equal(savedProduct.Price, result.Value.Price);
        Assert.Equal(savedProduct.IsActive, result.Value.IsActive);
        Assert.Equal(savedProduct.CreatedAt, result.Value.CreatedAt);
        Assert.Equal(savedProduct.UpdatedAt, result.Value.UpdatedAt);
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        public List<Product> AddedProducts { get; } = [];
        public int AddAsyncCallCount { get; private set; }

        public Task AddAsync(Product product)
        {
            AddAsyncCallCount++;
            AddedProducts.Add(product);
            return Task.CompletedTask;
        }
    }
}
