using Microsoft.AspNetCore.Authorization;

namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Requirement cho policy: user phải có permission trong JWT claims.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission ?? throw new ArgumentNullException(nameof(permission));
    }
}
