using DemoCICD.Contract.Abstractions.Shared;

namespace DemoCICD.Contract.Services.V1.User;

public static class Command
{
    public record CreateUserCommand(
        string UserName,
        string Email,
        string Password,
        string? FullName,
        IReadOnlyList<Guid> RoleIds) : ICommand<Guid>;

    public record UpdateUserCommand(
        Guid Id,
        string? Email,
        string? FullName,
        string? NewPassword,
        IReadOnlyList<Guid> RoleIds) : ICommand;

    public record DeleteUserCommand(Guid Id) : ICommand;
}
