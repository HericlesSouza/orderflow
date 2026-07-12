using FluentValidation;

namespace OrderFlow.Api.Orders.Create;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(request => request.Items)
            .NotNull()
            .NotEmpty();

        RuleForEach(request => request.Items)
            .SetValidator(new CreateOrderItemRequestValidator());

        RuleFor(request => request.Items)
            .Must(items => items is null || items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Each product can appear only once in an order.");
    }
}

public sealed class CreateOrderItemRequestValidator : AbstractValidator<CreateOrderItemRequest>
{
    public CreateOrderItemRequestValidator()
    {
        RuleFor(item => item.ProductId)
            .NotEqual(Guid.Empty);

        RuleFor(item => item.Quantity)
            .GreaterThan(0);
    }
}
