using Bogus;
using Microsoft.Extensions.DependencyInjection;
using OrderFlow.Domain.Entities;
using OrderFlow.Infrastructure.Persistence;

namespace OrderFlow.IntegrationTests.TestData;

public sealed class ProductTestDataBuilder
{
    private const int DefaultSeed = 12_345;
    private readonly IServiceProvider _serviceProvider;
    private readonly Faker _faker = new("en") { Random = new Randomizer(DefaultSeed) };
    private string? _name;
    private string? _description;
    private decimal _price = 3500.00m;
    private bool _isActive = true;

    public ProductTestDataBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ProductTestDataBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductTestDataBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProductTestDataBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public ProductTestDataBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public async Task<Product> PersistAsync(CancellationToken cancellationToken = default)
    {
        var product = Product.Create(
            _name ?? _faker.Commerce.ProductName(),
            _price,
            _description ?? _faker.Commerce.ProductDescription());

        if (!_isActive)
        {
            product.Deactivate();
        }

        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderFlowDbContext>();
        await dbContext.Products.AddAsync(product, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }
}
