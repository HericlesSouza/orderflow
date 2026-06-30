namespace OrderFlow.Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private Product() { }

    private Product(string name, decimal price, string description)
    {
        Validate(name, description, price);

        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Price = price;
        IsActive = true;
    }

    public static Product Create(string name, decimal price, string description)
    {
        return new Product(name, price, description);
    }

    public void Update(string name, decimal price, string description)
    {
        Validate(name, description, price);

        Name = name;
        Description = description;
        Price = price;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty.");

        if (price <= 0)
            throw new ArgumentException("Product price must be greater than zero.");
    }
}
