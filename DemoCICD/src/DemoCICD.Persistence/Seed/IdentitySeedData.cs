namespace DemoCICD.Persistence.Seed;

/// <summary>
/// Fixed GUIDs và IDs cho seed data - đảm bảo ổn định giữa các môi trường.
/// </summary>
internal static class IdentitySeedData
{
    #region Actions (string IDs)
    public const string ActionView = "VIEW";
    public const string ActionCreate = "CREATE";
    public const string ActionUpdate = "UPDATE";
    public const string ActionDelete = "DELETE";
    public const string ActionApprove = "APPROVE";
    public const string ActionExport = "EXPORT";
    #endregion

    #region Functions (string IDs)
    public const string FuncProduct = "PRODUCT";
    public const string FuncOrder = "ORDER";
    public const string FuncUser = "USER";
    public const string FuncReport = "REPORT";
    public const string FuncConfiguration = "CONFIGURATION";
    #endregion

    #region Roles (Guids)
    public static readonly Guid RoleSuperAdminId = new("11111111-1111-1111-1111-111111111111");
    public static readonly Guid RoleManagerId = new("22222222-2222-2222-2222-222222222222");
    public static readonly Guid RoleStaffId = new("33333333-3333-3333-3333-333333333333");
    #endregion

    #region Users (Guids)
    public static readonly Guid UserSuperAdminId = new("a1111111-1111-1111-1111-111111111111");
    public static readonly Guid UserManager1Id = new("b2222222-2222-2222-2222-222222222221");
    public static readonly Guid UserManager2Id = new("b2222222-2222-2222-2222-222222222222");
    public static readonly Guid UserStaff1Id = new("c3333333-3333-3333-3333-333333333331");
    public static readonly Guid UserStaff2Id = new("c3333333-3333-3333-3333-333333333332");
    public static readonly Guid UserStaff3Id = new("c3333333-3333-3333-3333-333333333333");
    public static readonly Guid UserMultiRoleId = new("d4444444-4444-4444-4444-444444444444");
    public static readonly Guid UserLimitedId = new("e5555555-5555-5555-5555-555555555555");
    public static readonly Guid UserNoApproveId = new("f6666666-6666-6666-6666-666666666666");
    #endregion

    #region Position (dummy for AppUser)
    public static readonly Guid PositionDefaultId = new("00000000-0000-0000-0000-000000000001");
    #endregion
}
