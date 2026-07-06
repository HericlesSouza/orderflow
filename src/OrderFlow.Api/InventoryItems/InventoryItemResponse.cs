using OrderFlow.Application.InventoryItems;

namespace OrderFlow.Api.InventoryItems;

public sealed record InventoryItemResponse(
    Guid Id,
    Guid ProductId,
    int Quantity,
    int ReservedQuantity,
    int AvailableQuantity,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static InventoryItemResponse FromOutput(InventoryItemOutput output)
    {
        return new InventoryItemResponse(
            output.Id,
            output.ProductId,
            output.Quantity,
            output.ReservedQuantity,
            output.AvailableQuantity,
            output.CreatedAt,
            output.UpdatedAt);
    }
}
