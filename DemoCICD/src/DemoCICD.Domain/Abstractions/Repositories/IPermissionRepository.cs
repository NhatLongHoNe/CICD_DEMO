namespace DemoCICD.Domain.Abstractions.Repositories;

/// <summary>
/// Lấy danh sách permission (FunctionId.ActionId) theo role IDs.
/// Dùng khi login để đưa vào JWT claims, không query mỗi request.
/// </summary>
public interface IPermissionRepository
{
    Task<IReadOnlyList<string>> GetPermissionsByRoleIdsAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default);
}
