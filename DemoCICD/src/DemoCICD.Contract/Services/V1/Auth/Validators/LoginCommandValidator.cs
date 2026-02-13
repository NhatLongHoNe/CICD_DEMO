using DemoCICD.Contract.Services.V1.Auth;
using FluentValidation;

namespace DemoCICD.Contract.Services.V1.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<Command.LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}
