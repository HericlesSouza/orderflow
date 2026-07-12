namespace OrderFlow.Application.InventoryItems.Increase;

public sealed record IncreaseInventoryItemStockInput(Guid ProductId, int Quantity);
