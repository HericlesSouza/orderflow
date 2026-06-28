using System;

namespace OrderFlow.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public decimal Price { get; private set; }
    public bool Active { get; private set; }

    private Product() { }

    private Product(string name, decimal price, bool active = false)
    {
        Id = Guid.NewGuid();
        Name = name;
        Price = price;
        Active = active;
    }

    public static Product Create(string name, decimal price, bool active = false)
    {
        var product = new Product(name, price, active);
        product.Validate();
        return product;
    }

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Product name cannot be empty.");

        if (Price <= 0)
            throw new ArgumentException("Product price must be greater than zero.");
    }
}
