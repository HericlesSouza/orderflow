using OrderFlow.Application.Products.UpdatePrice;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

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

    private sealed class FakeProductRepository(List<Product>? products = null) : IProductRepository
    {
        private readonly List<Product> _products = products ?? [];

        public int UpdateAsyncCallCount { get; private set; }

        public Task AddAsync(Product product, CancellationToken cancellationToken)
        {
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult(_products.SingleOrDefault(product => product.Id == id));
        }

        public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Product>>(_products);
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken)
        {
            UpdateAsyncCallCount++;
            return Task.CompletedTask;
        }
    }
}
