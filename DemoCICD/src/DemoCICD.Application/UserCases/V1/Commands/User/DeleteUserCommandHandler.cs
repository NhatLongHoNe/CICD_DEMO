using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.User;

public sealed class DeleteUserCommandHandler : ICommandHandler<Command.DeleteUserCommand>
{
    private readonly UserManager<AppUser> _userManager;

    public DeleteUserCommandHandler(UserManager<AppUser> userManager) => _userManager = userManager;

    public async Task<Result> Handle(Command.DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result.Failure(new Error("User.NotFound", "User not found."));

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return Result.Failure(new Error("User.DeleteFailed", string.Join("; ", result.Errors.Select(e => e.Description))));

        return Result.Success();
    }
}
