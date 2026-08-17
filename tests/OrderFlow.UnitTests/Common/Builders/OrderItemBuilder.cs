using OrderFlow.Domain.Entities;

namespace OrderFlow.UnitTests.Common.Builders;

internal sealed class OrderItemBuilder
{
    private Guid _productId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private int _quantity = 1;
    private decimal _unitPrice = 100.00m;

    public OrderItemBuilder ForProduct(Guid productId)
    {
        _productId = productId;
        return this;
    }

    public OrderItemBuilder WithQuantity(int quantity)
    {
        _quantity = quantity;
        return this;
    }

    public OrderItemBuilder WithUnitPrice(decimal unitPrice)
    {
        _unitPrice = unitPrice;
        return this;
    }

    public OrderItem Build()
    {
        return OrderItem.Create(_productId, _quantity, _unitPrice);
    }
}
