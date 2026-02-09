using DemoCICD.Contract.Services.V1.Auth;
using FluentValidation;

namespace DemoCICD.Contract.Services.V1.Auth.Validators;

public class RefreshTokenCommandValidator : AbstractValidator<Command.RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RefreshToken is required.");
    }
}
