using DemoCICD.Contract.Services.V1.Product;
using FluentValidation;

namespace DemoCICD.Contract.Services.V1.Product.Validators;

public class GetProductByIdValidator : AbstractValidator<Query.GetProductByIdQuery>
{
    public GetProductByIdValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
