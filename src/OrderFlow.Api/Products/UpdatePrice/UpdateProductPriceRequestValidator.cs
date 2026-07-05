using FluentValidation;

namespace OrderFlow.Api.Products.UpdatePrice;

public class UpdateProductPriceRequestValidator : AbstractValidator<UpdateProductPriceRequest>
{
    public UpdateProductPriceRequestValidator()
    {
        RuleFor(request => request.Price)
            .GreaterThan(0);
    }
}
