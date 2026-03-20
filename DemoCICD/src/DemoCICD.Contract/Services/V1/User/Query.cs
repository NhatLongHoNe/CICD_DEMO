using DemoCICD.Contract.Abstractions.Shared;
using static DemoCICD.Contract.Services.V1.User.Response;

namespace DemoCICD.Contract.Services.V1.User;

public static class Query
{
    public record GetUsersQuery(string? SearchTerm, int PageIndex, int PageSize) : IQuery<PagedResult<UserResponse>>;

    public record GetUserByIdQuery(Guid Id) : IQuery<UserResponse?>;

    public record GetRolesQuery : IQuery<IReadOnlyList<RoleItemResponse>>;

    public record GetFunctionsActionsQuery : IQuery<IReadOnlyList<FunctionActionNodeResponse>>;
}
