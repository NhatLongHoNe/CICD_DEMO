using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Persistence.Repositories;

internal sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;

    public RefreshTokenRepository(ApplicationDbContext context) => _context = context;

    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
    {
        await _context.RefreshTokens.AddAsync(refreshToken, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _context.RefreshTokens
            .AsTracking()
            .FirstOrDefaultAsync(rt => rt.Token == token, cancellationToken);
    }

    public void Update(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Update(refreshToken);
    }
}
