using DemoCICD.Contract.Abstractions.Shared;
using DemoCICD.Contract.Services.V1.User;
using DemoCICD.Persistence;
using Microsoft.EntityFrameworkCore;
using Action = DemoCICD.Domain.Entities.Identity.Action;

namespace DemoCICD.Application.UserCases.V1.Queries.User;

public sealed class GetFunctionsActionsQueryHandler : IQueryHandler<Query.GetFunctionsActionsQuery, IReadOnlyList<Response.FunctionActionNodeResponse>>
{
    private readonly ApplicationDbContext _context;

    public GetFunctionsActionsQueryHandler(ApplicationDbContext context) => _context = context;

    public async Task<Result<IReadOnlyList<Response.FunctionActionNodeResponse>>> Handle(Query.GetFunctionsActionsQuery request, CancellationToken cancellationToken)
    {
        var functions = await _context.Functions.AsNoTracking()
            .Where(f => f.IsActive == true)
            .OrderBy(f => f.SortOrder)
            .Select(f => new { f.Id, f.Name })
            .ToListAsync(cancellationToken);

        var actionInFunctions = await _context.ActionInFunctions.AsNoTracking()
            .ToListAsync(cancellationToken);

        var actionIds = actionInFunctions.Select(a => a.ActionId).Distinct().ToList();
        var actions = await _context.Set<Action>().AsNoTracking()
            .Where(a => actionIds.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, a => a.Name ?? a.Id, cancellationToken);

        var result = functions.Select(f =>
        {
            var actionIdsForFunc = actionInFunctions.Where(af => af.FunctionId == f.Id).Select(af => af.ActionId).ToList();
            var actionItems = actionIdsForFunc
                .Select(aid => new Response.ActionItemResponse(aid, actions.GetValueOrDefault(aid, aid)))
                .ToList();
            return new Response.FunctionActionNodeResponse(f.Id, f.Name ?? f.Id, actionItems);
        }).ToList();

        return Result.Success<IReadOnlyList<Response.FunctionActionNodeResponse>>(result);
    }
}
