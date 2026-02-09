using System.Security.Cryptography;
using DemoCICD.Application.Abstractions;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.Auth;

public sealed class LoginCommandHandler : ICommandHandler<Command.LoginCommand, Response.TokenResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        UserManager<AppUser> userManager,
        IAccessTokenService accessTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result<Response.TokenResponse>> Handle(Command.LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<Response.TokenResponse>(
                new Error("Auth.InvalidCredentials", "Invalid username or password."));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _accessTokenService.GenerateAccessToken(user.Id, user.UserName!, roles.ToList());

        var (refreshTokenValue, refreshTokenEntity) = GenerateRefreshToken(user.Id);
        await _refreshTokenRepository.AddAsync(refreshTokenEntity, cancellationToken);

        return Result.Success(new Response.TokenResponse(
            accessToken,
            refreshTokenValue,
            refreshTokenEntity.ExpiresAt));
    }

    private static (string Token, RefreshToken Entity) GenerateRefreshToken(Guid userId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return (token, RefreshToken.Create(userId, token, expiresAt));
    }
}
