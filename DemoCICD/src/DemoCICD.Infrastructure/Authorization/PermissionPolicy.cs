namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Helper tạo policy name cho permission (dùng với PermissionAuthorizationPolicyProvider).
/// </summary>
public static class PermissionPolicy
{
    public static string Name(string permission) => PermissionAuthorizationPolicyProvider.PermissionPolicyPrefix + permission;
}
