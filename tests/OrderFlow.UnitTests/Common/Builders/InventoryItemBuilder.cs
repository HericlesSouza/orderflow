using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Common.Builders;

internal sealed class InventoryItemBuilder
{
    private Guid _productId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private int _quantity = 10;
    private int _reservedQuantity;

    public InventoryItemBuilder ForProduct(Guid productId)
    {
        _productId = productId;
        return this;
    }

    public InventoryItemBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public InventoryItemBuilder WithReservedQuantity(int reservedQuantity)
    {
        _reservedQuantity = reservedQuantity;
        return this;
    }

    public InventoryItem Build()
    {
        var inventoryItem = InventoryItem.Create(_productId, _quantity);

        if (_reservedQuantity > 0)
        {
            inventoryItem.Reserve(_reservedQuantity);
        }

        return inventoryItem;
    }
}
