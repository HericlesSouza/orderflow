using FluentValidation;

namespace OrderFlow.Api.Products.UpdateStatus;

public class UpdateProductStatusRequestValidator : AbstractValidator<UpdateProductStatusRequest>
{
    public UpdateProductStatusRequestValidator()
    {
        RuleFor(request => request.IsActive)
            .NotNull();
    }
}
