using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Custom AuthorizationHandler: kiểm tra permission từ JWT claims.
/// Không query DB mỗi request. Trả 403 nếu thiếu quyền.
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            context.Fail();
            return Task.CompletedTask;
        }

        var permissionClaims = context.User
            .FindAll(PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (permissionClaims.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail(new AuthorizationFailureReason(this, "Forbidden: insufficient permissions"));
        }

        return Task.CompletedTask;
    }
}
