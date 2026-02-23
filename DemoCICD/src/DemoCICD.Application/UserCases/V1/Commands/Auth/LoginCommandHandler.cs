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
    private readonly RoleManager<AppRole> _roleManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IPermissionRepository _permissionRepository;

    public LoginCommandHandler(
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

    public async Task<Result<Response.TokenResponse>> Handle(Command.LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null || !await _userManager.CheckPasswordAsync(user, request.Password))
        {
            return Result.Failure<Response.TokenResponse>(
                new Error("Auth.InvalidCredentials", "Invalid username or password."));
        }

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
