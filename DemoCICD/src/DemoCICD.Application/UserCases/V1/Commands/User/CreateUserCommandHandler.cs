using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.User;

public sealed class CreateUserCommandHandler : ICommandHandler<Command.CreateUserCommand, Guid>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;

    public CreateUserCommandHandler(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<Guid>> Handle(Command.CreateUserCommand request, CancellationToken cancellationToken)
    {
        var existing = await _userManager.FindByNameAsync(request.UserName);
        if (existing != null)
            return Result.Failure<Guid>(new Error("User.DuplicateUserName", "Username already exists."));

        if (!string.IsNullOrEmpty(request.Email))
        {
            var existingEmail = await _userManager.FindByEmailAsync(request.Email);
            if (existingEmail != null)
                return Result.Failure<Guid>(new Error("User.DuplicateEmail", "Email already exists."));
        }

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
            return Result.Failure<Guid>(new Error("User.CreateFailed", string.Join("; ", createResult.Errors.Select(e => e.Description))));

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
                var addRolesResult = await _userManager.AddToRolesAsync(user, rolesToAdd);
                if (!addRolesResult.Succeeded)
                    return Result.Failure<Guid>(new Error("User.AddRolesFailed", string.Join("; ", addRolesResult.Errors.Select(e => e.Description))));
            }
        }

        return Result.Success(user.Id);
    }
}
