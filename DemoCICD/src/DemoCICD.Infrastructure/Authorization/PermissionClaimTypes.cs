namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Claim type cho permission trong JWT.
/// Handler đọc claim này để kiểm tra quyền, không query DB.
/// </summary>
public static class PermissionClaimTypes
{
    public const string Permission = "permission";
}
