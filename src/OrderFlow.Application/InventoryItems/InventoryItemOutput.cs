using OrderFlow.Domain.Entities;

namespace OrderFlow.Application.InventoryItems;

public sealed record InventoryItemOutput(
    Guid Id,
    Guid ProductId,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static InventoryItemOutput FromEntity(InventoryItem inventoryItem)
    {
        return new InventoryItemOutput(
            inventoryItem.Id,
            inventoryItem.ProductId,
            inventoryItem.Quantity,
            inventoryItem.ReservedQuantity,
            inventoryItem.AvailableQuantity,
            inventoryItem.CreatedAt,
            inventoryItem.UpdatedAt);
    }
}
