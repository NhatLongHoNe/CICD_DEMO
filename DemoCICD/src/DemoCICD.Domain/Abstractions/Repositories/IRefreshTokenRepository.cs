using DemoCICD.Domain.Entities.Identity;

namespace DemoCICD.Domain.Abstractions.Repositories;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    void Update(RefreshToken refreshToken);
}
