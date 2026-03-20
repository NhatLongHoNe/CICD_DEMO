using DemoCICD.Contract.Abstractions.Shared;

namespace DemoCICD.Contract.Services.V1.User;

public static class Response
{
    public record UserResponse(
        Guid Id,
        string UserName,
        string? Email,
        string? FullName,
        bool EmailConfirmed,
        bool LockoutEnabled,
        DateTimeOffset? LockoutEnd,
        IReadOnlyList<string> Roles);

    public record RoleItemResponse(Guid Id, string Name, string? Description, string? RoleCode);

    public record FunctionActionNodeResponse(string FunctionId, string FunctionName, IReadOnlyList<ActionItemResponse> Actions);

    public record ActionItemResponse(string ActionId, string ActionName);
}
