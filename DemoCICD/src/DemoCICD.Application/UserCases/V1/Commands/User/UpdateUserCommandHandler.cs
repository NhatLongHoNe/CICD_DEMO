using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.User;

public sealed class UpdateUserCommandHandler : ICommandHandler<Command.UpdateUserCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public UpdateUserCommandHandler(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result> Handle(Command.UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.Id.ToString());
        if (user == null)
            return Result.Failure(new Error("User.NotFound", "User not found."));

        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null && existingEmail.Id != user.Id)
                return Result.Failure(new Error("User.DuplicateEmail", "Email already in use."));
            user.Email = request.Email;
        }

        if (request.FullName != null)
            user.FullName = request.FullName;

        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, token, request.NewPassword);
            if (!resetResult.Succeeded)
                return Result.Failure(new Error("User.PasswordResetFailed", string.Join("; ", resetResult.Errors.Select(e => e.Description))));
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return Result.Failure(new Error("User.UpdateFailed", string.Join("; ", updateResult.Errors.Select(e => e.Description))));

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            return Result.Failure(new Error("User.UpdateRolesFailed", string.Join("; ", removeResult.Errors.Select(e => e.Description))));

        if (request.RoleIds.Count > 0)
        {
            var rolesToAdd = new List<string>();
            foreach (var roleId in request.RoleIds)
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role != null)
                    rolesToAdd.Add(role.Name!);
            }

            if (rolesToAdd.Count > 0)
            {
                var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addResult.Succeeded)
                    return Result.Failure(new Error("User.AddRolesFailed", string.Join("; ", addResult.Errors.Select(e => e.Description))));
            }
        }

        return Result.Success();
    }
}
