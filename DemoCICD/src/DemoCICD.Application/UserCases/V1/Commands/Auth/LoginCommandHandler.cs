using System.Security.Cryptography;
using DemoCICD.Application.Abstractions;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            if (user != null)
                await _userManager.AccessFailedAsync(user);
            return Result.Failure<Response.TokenResponse>(
                new Error("Auth.InvalidCredentials", "Invalid username or password."));
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            return Result.Failure<Response.TokenResponse>(new Error("Auth.LockedOut",
                $"Account locked until {lockoutEnd:O}. Try again later."));
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var roleNames = roles.ToList();
        var roleIds = roleNames.Count == 0
            ? new List<Guid>()
            : (await _roleManager.Roles
                .Where(r => roleNames.Contains(r.Name!))
                .Select(r => r.Id)
                .ToListAsync(cancellationToken));

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
