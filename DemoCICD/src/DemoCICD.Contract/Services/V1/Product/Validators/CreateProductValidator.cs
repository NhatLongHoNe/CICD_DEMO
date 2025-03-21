using DemoCICD.Contract.Services.V1.Product;
using FluentValidation;

namespace DemoCICD.Contract.Services.V1.Product.Validators;

public class CreateProductValidator : AbstractValidator<Command.CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Description).NotEmpty();
    }
}
