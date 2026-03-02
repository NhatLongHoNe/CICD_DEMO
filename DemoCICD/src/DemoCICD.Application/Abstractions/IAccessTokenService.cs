namespace DemoCICD.Application.Abstractions;

public interface IAccessTokenService
{
    /// <summary>
    /// Generates JWT with roles and permissions in claims.
    /// Permissions are read from claims at runtime (no DB query per request).
    /// </summary>
    string GenerateAccessToken(
        Guid userId,
        string userName,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions);
}
