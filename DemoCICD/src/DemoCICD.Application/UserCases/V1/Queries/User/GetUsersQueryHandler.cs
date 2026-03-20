using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using DemoCICD.Domain.Abstractions.Repositories;
using DemoCICD.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Application.UserCases.V1.Queries.User;

public sealed class GetUsersQueryHandler : IQueryHandler<Query.GetUsersQuery, PagedResult<Response.UserResponse>>
{
    private readonly ApplicationDbContext _context;
    private readonly IUserRoleNamesRepository _roleNamesRepo;

    public GetUsersQueryHandler(ApplicationDbContext context, IUserRoleNamesRepository roleNamesRepo)
    {
        _context = context;
        _roleNamesRepo = roleNamesRepo;
    }

    public async Task<Result<PagedResult<Response.UserResponse>>> Handle(Query.GetUsersQuery request, CancellationToken cancellationToken)
    {
        var term = request.SearchTerm?.Trim();
        var query = _context.Set<AppUser>().AsNoTracking()
            .Where(u => string.IsNullOrEmpty(term)
                || u.UserName != null && u.UserName.Contains(term)
                || u.Email != null && u.Email.Contains(term)
                || u.FullName != null && u.FullName.Contains(term));

        var totalCount = await query.CountAsync(cancellationToken);
        var pageIndex = request.PageIndex <= 0 ? PagedResult<Response.UserResponse>.DefaultPageIndex : request.PageIndex;
        var pageSize = request.PageSize <= 0 ? PagedResult<Response.UserResponse>.DefaultPageSize
            : request.PageSize > PagedResult<Response.UserResponse>.UpperPageSize ? PagedResult<Response.UserResponse>.UpperPageSize : request.PageSize;

        var userList = await query
            .OrderBy(u => u.UserName)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new { u.Id, u.UserName, u.Email, u.FullName, u.EmailConfirmed, u.LockoutEnabled, u.LockoutEnd })
            .ToListAsync(cancellationToken);

        var userIds = userList.Select(x => x.Id).ToList();
        var roleNamesByUser = await _roleNamesRepo.GetRoleNamesByUserIdsAsync(userIds, cancellationToken);

        var items = userList.Select(u => new Response.UserResponse(
            u.Id,
            u.UserName!,
            u.Email,
            u.FullName,
            u.EmailConfirmed,
            u.LockoutEnabled,
            u.LockoutEnd,
            roleNamesByUser.TryGetValue(u.Id, out var names) ? names : new List<string>())).ToList();

        var paged = PagedResult<Response.UserResponse>.Create(items, pageIndex, pageSize, totalCount);
        return Result.Success(paged);
    }
}
