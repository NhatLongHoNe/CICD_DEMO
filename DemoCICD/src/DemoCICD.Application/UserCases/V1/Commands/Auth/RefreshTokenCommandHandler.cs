using System.Security.Cryptography;
using DemoCICD.Application.Abstractions;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.Auth;

public sealed class RefreshTokenCommandHandler : ICommandHandler<Command.RefreshTokenCommand, Response.TokenResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPermissionRepository _permissionRepository;

    public RefreshTokenCommandHandler(
        UserManager<AppUser> userManager,
        RoleManager<AppRole> roleManager,
        IAccessTokenService accessTokenService,
        IRefreshTokenRepository refreshTokenRepository,
        IPermissionRepository permissionRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _accessTokenService = accessTokenService;
        _refreshTokenRepository = refreshTokenRepository;
        _permissionRepository = permissionRepository;
    }

    public async Task<Result<Response.TokenResponse>> Handle(Command.RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (storedToken is null || !storedToken.IsActive)
        {
            return Result.Failure<Response.TokenResponse>(
                new Error("Auth.InvalidRefreshToken", "Invalid or expired refresh token."));
        }

        var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
        if (user is null)
        {
            return Result.Failure<Response.TokenResponse>(
                new Error("Auth.UserNotFound", "User no longer exists."));
        }

        storedToken.Revoke();

        var roles = await _userManager.GetRolesAsync(user);
        var roleIds = new List<Guid>();
        foreach (var roleName in roles)
        {
            var role = await _roleManager.FindByNameAsync(roleName);
            if (role != null)
                roleIds.Add(role.Id);
        }

        var permissions = await _permissionRepository.GetPermissionsByRoleIdsAsync(roleIds, cancellationToken);
        var accessToken = _accessTokenService.GenerateAccessToken(user.Id, user.UserName!, roles.ToList(), permissions);

        var (newRefreshTokenValue, newRefreshTokenEntity) = GenerateRefreshToken(user.Id);
        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, cancellationToken);

        return Result.Success(new Response.TokenResponse(
            accessToken,
            newRefreshTokenValue,
            newRefreshTokenEntity.ExpiresAt));
    }

    private static (string Token, RefreshToken Entity) GenerateRefreshToken(Guid userId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(tokenBytes);
        var expiresAt = DateTime.UtcNow.AddDays(7);

        return (token, RefreshToken.Create(userId, token, expiresAt));
    }
}
