namespace DemoCICD.Infrastructure.Authorization;

/// <summary>
/// Permission constants cho Product module (format: FunctionId.ActionId).
/// Khớp với dữ liệu RBAC seed (Actions: VIEW, CREATE, UPDATE, DELETE; Functions: PRODUCT).
/// </summary>
public static class ProductPermissions
{
    public const string View = "PRODUCT.VIEW";
    public const string Create = "PRODUCT.CREATE";
    public const string Update = "PRODUCT.UPDATE";
    public const string Delete = "PRODUCT.DELETE";
}
