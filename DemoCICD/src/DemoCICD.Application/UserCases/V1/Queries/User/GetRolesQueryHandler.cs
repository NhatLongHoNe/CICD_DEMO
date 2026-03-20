using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Domain.Entities.Identity;
using DemoCICD.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DemoCICD.Application.UserCases.V1.Queries.User;

public sealed class GetRolesQueryHandler : IQueryHandler<Query.GetRolesQuery, IReadOnlyList<Response.RoleItemResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetRolesQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<Response.RoleItemResponse>>> Handle(Query.GetRolesQuery request, CancellationToken cancellationToken)
    {
        var list = await _context.Set<AppRole>().AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new Response.RoleItemResponse(r.Id, r.Name!, r.Description, r.RoleCode))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<Response.RoleItemResponse>>(list);
    }
}
