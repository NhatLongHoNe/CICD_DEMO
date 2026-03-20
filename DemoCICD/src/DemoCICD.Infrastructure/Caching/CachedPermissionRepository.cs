using System.Text.Json;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Persistence.Repositories;
using Microsoft.Extensions.Caching.Distributed;

namespace DemoCICD.Infrastructure.Caching;

/// <summary>
/// Cache-aside cho permissions theo role IDs. TTL 15 phút; invalidate khi admin đổi quyền.
/// </summary>
public sealed class CachedPermissionRepository : IPermissionRepository
{
    private const string KeyPrefix = "perms:roles:";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);

    private readonly PermissionRepository _inner;
    private readonly IDistributedCache _cache;

    public CachedPermissionRepository(PermissionRepository inner, IDistributedCache cache)
    {
        _inner = inner;
        _cache = cache;
    }

    public async Task<IReadOnlyList<string>> GetPermissionsByRoleIdsAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
            return Array.Empty<string>();

        var key = KeyPrefix + string.Join(",", roleIds.OrderBy(x => x));
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (cached != null)
        {
            var list = JsonSerializer.Deserialize<IReadOnlyList<string>>(cached);
            return list ?? Array.Empty<string>();
        }

        var permissions = await _inner.GetPermissionsByRoleIdsAsync(roleIds, cancellationToken);
        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(permissions),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl },
            cancellationToken);

        return permissions;
    }
}
