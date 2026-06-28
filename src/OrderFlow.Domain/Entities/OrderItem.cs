namespace OrderFlow.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => UnitPrice * Quantity;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; private set; } = DateTime.UtcNow;

    private OrderItem() { }

    private OrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        Validate(productId, quantity, unitPrice);

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }

    public static OrderItem Create(Guid productId, int quantity, decimal unitPrice)
    {
        return new OrderItem(productId, quantity, unitPrice);
    }

    private static void Validate(Guid productId, int quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID cannot be empty.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.");

        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be greater than zero.");
    }
}
