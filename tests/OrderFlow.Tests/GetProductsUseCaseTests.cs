using OrderFlow.Application.Products.GetAll;
using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests;

public sealed class GetProductsUseCaseTests
{
    [Fact]
    public async Task Should_Return_Empty_List_When_There_Are_No_Products()
    {
        var repository = new FakeProductRepository();
        var useCase = new GetProductsUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Should_Return_Products_Mapped_To_Output_In_Repository_Order()
    {
        var firstProduct = Product.Create("Notebook", 3500.00m, "Gaming notebook");
        var secondProduct = Product.Create("Mouse", 150.00m, "Wireless mouse");
        var repository = new FakeProductRepository([secondProduct, firstProduct]);
        var useCase = new GetProductsUseCase(repository);

        var result = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.Equal(secondProduct.Id, result[0].Id);
        Assert.Equal("Mouse", result[0].Name);
        Assert.Equal(firstProduct.Id, result[1].Id);
        Assert.Equal("Notebook", result[1].Name);
    }

    private sealed class FakeProductRepository(List<Product>? products = null) : IProductRepository
    {
        private readonly List<Product> _products = products ?? [];

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
            return Task.CompletedTask;
        }
    }
}
