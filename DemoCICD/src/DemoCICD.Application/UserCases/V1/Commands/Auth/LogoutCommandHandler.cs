using System.Security.Cryptography;
using DemoCICD.Application.Abstractions;
using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.Auth;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace DemoCICD.Application.UserCases.V1.Commands.Auth;

public sealed class LogoutCommandHandler : ICommandHandler<Command.LogoutCommand>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly IAccessTokenService _accessTokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(
        UserManager<AppUser> userManager,
        IAccessTokenService accessTokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _accessTokenService = accessTokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<Result> Handle(Command.LogoutCommand request, CancellationToken cancellationToken)
    {
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken);

        if (storedToken != null && storedToken.IsActive)
        {
            storedToken.Revoke();
        }

        return Result.Success();
    }
}
