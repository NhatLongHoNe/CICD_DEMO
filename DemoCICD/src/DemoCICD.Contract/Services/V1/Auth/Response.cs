namespace DemoCICD.Contract.Services.V1.Auth;

public static class Response
{
    public record TokenResponse(
        string AccessToken,
        string RefreshToken,
        DateTime ExpiresAt);
}
