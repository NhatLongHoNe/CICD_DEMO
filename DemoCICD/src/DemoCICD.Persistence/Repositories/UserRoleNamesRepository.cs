using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Persistence.Repositories;

public sealed class UserRoleNamesRepository : IUserRoleNamesRepository
{
    private readonly ApplicationDbContext _context;

    public UserRoleNamesRepository(ApplicationDbContext context) => _context = context;

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<string>>> GetRoleNamesByUserIdsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<string>>();

        var list = await _context.Set<IdentityUserRole<Guid>>()
            .AsNoTracking()
            .Where(ur => userIds.Contains(ur.UserId))
            .Join(_context.Set<AppRole>().AsNoTracking(), ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .ToListAsync(cancellationToken);

        return list
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());
    }
}
