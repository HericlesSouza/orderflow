namespace OrderFlow.Application.InventoryItems.Create;

public sealed record CreateInventoryItemInput(Guid ProductId, int Quantity);
