namespace OrderFlow.Domain.Entities;

public class Product
{
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 1000;

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

    public void ChangePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Product price must be greater than zero.");

        if (Price == newPrice)
            return;

        Price = newPrice;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        if (isActive)
        {
            Activate();
            return;
        }

        Deactivate();
    }

    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(string name, string description, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name cannot be empty.");

        if (name.Length > NameMaxLength)
            throw new ArgumentException($"Product name cannot be longer than {NameMaxLength} characters.");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Product description cannot be empty.");

        if (description.Length > DescriptionMaxLength)
            throw new ArgumentException($"Product description cannot be longer than {DescriptionMaxLength} characters.");

        if (price <= 0)
            throw new ArgumentException("Product price must be greater than zero.");
    }
}
