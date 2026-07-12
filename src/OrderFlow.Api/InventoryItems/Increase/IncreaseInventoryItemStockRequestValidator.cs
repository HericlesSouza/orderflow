using FluentValidation;

namespace OrderFlow.Api.InventoryItems.Increase;

public class IncreaseInventoryItemStockRequestValidator : AbstractValidator<IncreaseInventoryItemStockRequest>
{
    public IncreaseInventoryItemStockRequestValidator()
    {
        RuleFor(request => request.Quantity)
            .GreaterThan(0);
    }
}
