namespace OrderFlow.Application.InventoryItems;

public sealed class DuplicateInventoryItemException(Guid productId)
    : Exception("An inventory item already exists for this product.")
{
    public Guid ProductId { get; } = productId;
}
