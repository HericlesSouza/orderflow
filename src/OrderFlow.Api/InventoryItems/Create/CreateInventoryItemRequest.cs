namespace OrderFlow.Api.InventoryItems.Create;

public sealed record CreateInventoryItemRequest(Guid ProductId, int Quantity);
