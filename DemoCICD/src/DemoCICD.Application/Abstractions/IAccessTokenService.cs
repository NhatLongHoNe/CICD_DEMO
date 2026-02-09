namespace DemoCICD.Application.Abstractions;

public interface IAccessTokenService
{
    string GenerateAccessToken(Guid userId, string userName, IReadOnlyList<string> roles);
}
