namespace OrderFlow.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private InventoryItem() { }

    private InventoryItem(Guid productId, int quantity)
    {
        Validate(productId, quantity);

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
    }

    public static InventoryItem Create(Guid productId, int quantity)
    {
        return new InventoryItem(productId, quantity);
    }

    public void UpdateQuantity(int quantity)
    {
        Validate(ProductId, quantity);

        Quantity = quantity;
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
