using FluentValidation;

namespace OrderFlow.Api.InventoryItems.Create;

public class CreateInventoryItemRequestValidator : AbstractValidator<CreateInventoryItemRequest>
{
    public CreateInventoryItemRequestValidator()
    {
        RuleFor(request => request.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(request => request.Quantity)
            .GreaterThanOrEqualTo(0);
    }
}
