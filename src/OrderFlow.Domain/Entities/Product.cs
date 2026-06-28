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
        Id = Guid.NewGuid();
        Name = name;
        Description = description;
        Price = price;
        IsActive = true;

        Validate();
    }

    public static Product Create(string name, decimal price, string description)
    {
        return new Product(name, price, description);
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

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new ArgumentException("Product name cannot be empty.");

        if (Price <= 0)
            throw new ArgumentException("Product price must be greater than zero.");
    }
}
