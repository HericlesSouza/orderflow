using FluentValidation;
using OrderFlow.Domain.Entities;

namespace OrderFlow.Api.Products.Create;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(request => request.Name)
            .NotEmpty()
            .MaximumLength(Product.NameMaxLength);

        RuleFor(request => request.Description)
            .NotEmpty()
            .MaximumLength(Product.DescriptionMaxLength);

        RuleFor(request => request.Price)
            .GreaterThan(0);
    }
}
