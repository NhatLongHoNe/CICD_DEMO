using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Application.UserCases.V1.Queries.User;

public sealed class GetUserByIdQueryHandler : IQueryHandler<Query.GetUserByIdQuery, Response.UserResponse?>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRoleNamesRepository _roleNamesRepo;

    public GetUserByIdQueryHandler(ApplicationDbContext context, IUserRoleNamesRepository roleNamesRepo)
    {
        _context = context;
        _roleNamesRepo = roleNamesRepo;
    }

    public async Task<Result<Response.UserResponse?>> Handle(Query.GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var u = await _context.Set<AppUser>().AsNoTracking()
            .Where(user => user.Id == request.Id)
            .Select(user => new { user.Id, user.UserName, user.Email, user.FullName, user.EmailConfirmed, user.LockoutEnabled, user.LockoutEnd })
            .FirstOrDefaultAsync(cancellationToken);

        if (u == null)
        {
            return Result.Success<Response.UserResponse?>(null);
        }

        var roleNamesByUser = await _roleNamesRepo.GetRoleNamesByUserIdsAsync(new[] { request.Id }, cancellationToken);
        var roleNames = roleNamesByUser.TryGetValue(request.Id, out var names) ? names : new List<string>();

        var response = new Response.UserResponse(
            u.Id,
            u.UserName!,
            u.Email,
            u.FullName,
            u.EmailConfirmed,
            u.LockoutEnabled,
            u.LockoutEnd,
            roleNames);

        return Result.Success<Response.UserResponse?>(response);
    }
}
