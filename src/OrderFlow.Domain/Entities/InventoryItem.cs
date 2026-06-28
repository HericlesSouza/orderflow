namespace OrderFlow.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int AvailableQuantity => Quantity - ReservedQuantity;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private InventoryItem() { }

    private InventoryItem(Guid productId, int quantity)
    {
        Validate(productId, quantity);

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        ReservedQuantity = 0;
    }

    public static InventoryItem Create(Guid productId, int quantity)
    {
        return new InventoryItem(productId, quantity);
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Reserve quantity must be greater than zero.");

        if (quantity > AvailableQuantity)
            throw new InvalidOperationException("Not enough available quantity to reserve.");

        ReservedQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Release(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Release quantity must be greater than zero.");

        if (quantity > ReservedQuantity)
            throw new InvalidOperationException("Not enough reserved quantity to release.");

        ReservedQuantity -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Increase quantity must be greater than zero.");

        Quantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void Validate(Guid productId, int quantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("InventoryItem product id cannot be empty.");

        if (quantity < 0)
            throw new ArgumentException("InventoryItem quantity cannot be negative.");
    }
}
