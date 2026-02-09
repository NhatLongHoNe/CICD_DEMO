using DemoCICD.Contract.Abstractions.Shared;

namespace DemoCICD.Contract.Services.V1.Auth;

public static class Command
{
    public record LoginCommand(string UserName, string Password) : ICommand<Response.TokenResponse>;

    public record RefreshTokenCommand(string RefreshToken) : ICommand<Response.TokenResponse>;
}
