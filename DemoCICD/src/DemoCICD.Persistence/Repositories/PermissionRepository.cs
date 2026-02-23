using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Persistence.Repositories;

internal sealed class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyList<string>> GetPermissionsByRoleIdsAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        if (roleIds.Count == 0)
            return Array.Empty<string>();

        var permissions = await _context.Permissions
            .AsNoTracking()
            .Where(p => roleIds.Contains(p.RoleId))
            .Select(p => $"{p.FunctionId}.{p.ActionId}")
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
