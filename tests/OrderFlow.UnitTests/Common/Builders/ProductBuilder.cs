using Bogus;
using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Common.Builders;

internal sealed class ProductBuilder
{
    private readonly Faker _faker = new("en") { Random = new Randomizer(12_345) };
    private string? _name;
    private string? _description;
    private decimal _price = 3500.00m;
    private bool _isActive = true;

    public ProductBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public ProductBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public ProductBuilder WithPrice(decimal price)
    {
        _price = price;
        return this;
    }

    public ProductBuilder Inactive()
    {
        _isActive = false;
        return this;
    }

    public Product Build()
    {
        var product = Product.Create(
            _name ?? _faker.Commerce.ProductName(),
            _price,
            _description ?? _faker.Commerce.ProductDescription());

        if (!_isActive)
        {
            product.Deactivate();
        }

        return product;
    }
}
