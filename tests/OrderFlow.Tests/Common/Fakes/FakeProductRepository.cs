using OrderFlow.Application.Repositories;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Tests.Common.Fakes;

public sealed class FakeProductRepository(List<Product>? products = null) : IProductRepository
{
    private readonly List<Product> _products = products ?? [];

    public IReadOnlyList<Product> Products => _products;
    public int AddAsyncCallCount { get; private set; }
    public int UpdateAsyncCallCount { get; private set; }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        AddAsyncCallCount++;
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
