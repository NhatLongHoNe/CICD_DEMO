using Microsoft.AspNetCore.Authorization;

namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Attribute để yêu cầu permission cho controller/endpoint.
/// Dùng với policy name: [Authorize(Policy = "Product.View")].
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(policy: permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
