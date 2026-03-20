namespace DemoCICD.Domain.Abstractions.Repositories;

/// <summary>
/// Lấy danh sách tên role theo danh sách UserId (dùng cho list user).
/// </summary>
public interface IUserRoleNamesRepository
{
    Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRoleNamesByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default);
}
