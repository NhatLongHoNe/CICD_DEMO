using DemoCICD.Persistence.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoCICD.Persistence.Migrations;

/// <summary>
/// Seed RBAC: Action → Function → ActionInFunction → Role → Permission.
/// Dùng raw SQL để không phụ thuộc model (Designer data-only).
/// Users + UserRoles được seed bởi IdentityDataSeeder (UserManager hash password).
/// </summary>
public partial class SeedRbacData : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. ACTIONS (6 actions chuẩn enterprise)
        migrationBuilder.Sql(@"
            INSERT INTO Actions (Id, Name, SortOrder, IsActive) VALUES
            ('VIEW', 'View', 1, 1),
            ('CREATE', 'Create', 2, 1),
            ('UPDATE', 'Update', 3, 1),
            ('DELETE', 'Delete', 4, 1),
            ('APPROVE', 'Approve', 5, 1),
            ('EXPORT', 'Export', 6, 1)");

        // 2. FUNCTIONS (5 modules)
        migrationBuilder.Sql(@"
            INSERT INTO Functions (Id, Name, Url, ParrentId, SortOrder, CssClass, IsActive) VALUES
            ('PRODUCT', 'Product', '/products', 'ROOT', 1, 'fa-box', 1),
            ('ORDER', 'Order', '/orders', 'ROOT', 2, 'fa-shopping-cart', 1),
            ('USER', 'User', '/users', 'ROOT', 3, 'fa-users', 1),
            ('REPORT', 'Report', '/reports', 'ROOT', 4, 'fa-chart-bar', 1),
            ('CONFIGURATION', 'Configuration', '/config', 'ROOT', 5, 'fa-cog', 1)");

        // 3. ACTION_IN_FUNCTIONS
        migrationBuilder.Sql(@"
            INSERT INTO ActionInFunctions (ActionId, FunctionId) VALUES
            ('VIEW', 'PRODUCT'), ('CREATE', 'PRODUCT'), ('UPDATE', 'PRODUCT'), ('DELETE', 'PRODUCT'), ('EXPORT', 'PRODUCT'),
            ('VIEW', 'ORDER'), ('CREATE', 'ORDER'), ('UPDATE', 'ORDER'), ('DELETE', 'ORDER'), ('APPROVE', 'ORDER'), ('EXPORT', 'ORDER'),
            ('VIEW', 'USER'), ('CREATE', 'USER'), ('UPDATE', 'USER'), ('DELETE', 'USER'),
            ('VIEW', 'REPORT'), ('EXPORT', 'REPORT'),
            ('VIEW', 'CONFIGURATION'), ('UPDATE', 'CONFIGURATION')");

        // 4. ROLES
        var saGuid = IdentitySeedData.RoleSuperAdminId;
        var mgrGuid = IdentitySeedData.RoleManagerId;
        var stfGuid = IdentitySeedData.RoleStaffId;
        migrationBuilder.Sql($@"
            INSERT INTO AppRoles (Id, Name, NormalizedName, Description, RoleCode, ConcurrencyStamp) VALUES
            ('{saGuid}', 'SuperAdmin', 'SUPERADMIN', 'Full quyền hệ thống', 'SA', NEWID()),
            ('{mgrGuid}', 'Manager', 'MANAGER', 'CRUD + Approve, không quản lý User', 'MGR', NEWID()),
            ('{stfGuid}', 'Staff', 'STAFF', 'View + Create, không Update/Delete/Approve', 'STF', NEWID())");

        // 5. PERMISSIONS (phân quyền không đối xứng)
        InsertPermissionsSql(migrationBuilder);
    }

    private static void InsertPermissionsSql(MigrationBuilder migrationBuilder)
    {
        var sa = "11111111-1111-1111-1111-111111111111";
        var mgr = "22222222-2222-2222-2222-222222222222";
        var stf = "33333333-3333-3333-3333-333333333333";

        migrationBuilder.Sql($@"
            INSERT INTO Permissions (RoleId, FunctionId, ActionId) VALUES
            -- SuperAdmin: full
            ('{sa}', 'PRODUCT', 'VIEW'), ('{sa}', 'PRODUCT', 'CREATE'), ('{sa}', 'PRODUCT', 'UPDATE'), ('{sa}', 'PRODUCT', 'DELETE'), ('{sa}', 'PRODUCT', 'EXPORT'),
            ('{sa}', 'ORDER', 'VIEW'), ('{sa}', 'ORDER', 'CREATE'), ('{sa}', 'ORDER', 'UPDATE'), ('{sa}', 'ORDER', 'DELETE'), ('{sa}', 'ORDER', 'APPROVE'), ('{sa}', 'ORDER', 'EXPORT'),
            ('{sa}', 'USER', 'VIEW'), ('{sa}', 'USER', 'CREATE'), ('{sa}', 'USER', 'UPDATE'), ('{sa}', 'USER', 'DELETE'),
            ('{sa}', 'REPORT', 'VIEW'), ('{sa}', 'REPORT', 'EXPORT'),
            ('{sa}', 'CONFIGURATION', 'VIEW'), ('{sa}', 'CONFIGURATION', 'UPDATE'),
            -- Manager: full trừ USER
            ('{mgr}', 'PRODUCT', 'VIEW'), ('{mgr}', 'PRODUCT', 'CREATE'), ('{mgr}', 'PRODUCT', 'UPDATE'), ('{mgr}', 'PRODUCT', 'DELETE'), ('{mgr}', 'PRODUCT', 'EXPORT'),
            ('{mgr}', 'ORDER', 'VIEW'), ('{mgr}', 'ORDER', 'CREATE'), ('{mgr}', 'ORDER', 'UPDATE'), ('{mgr}', 'ORDER', 'DELETE'), ('{mgr}', 'ORDER', 'APPROVE'), ('{mgr}', 'ORDER', 'EXPORT'),
            ('{mgr}', 'REPORT', 'VIEW'), ('{mgr}', 'REPORT', 'EXPORT'),
            ('{mgr}', 'CONFIGURATION', 'VIEW'), ('{mgr}', 'CONFIGURATION', 'UPDATE'),
            -- Staff: chỉ View + Create
            ('{stf}', 'PRODUCT', 'VIEW'), ('{stf}', 'PRODUCT', 'CREATE'),
            ('{stf}', 'ORDER', 'VIEW'), ('{stf}', 'ORDER', 'CREATE'),
            ('{stf}', 'REPORT', 'VIEW')");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM Permissions WHERE RoleId IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333')");
        migrationBuilder.Sql("DELETE FROM AppUserRoles WHERE RoleId IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333')");
        migrationBuilder.Sql("DELETE FROM AppRoles WHERE Id IN ('11111111-1111-1111-1111-111111111111','22222222-2222-2222-2222-222222222222','33333333-3333-3333-3333-333333333333')");
        migrationBuilder.Sql("DELETE FROM ActionInFunctions WHERE ActionId IN ('VIEW','CREATE','UPDATE','DELETE','APPROVE','EXPORT')");
        migrationBuilder.Sql("DELETE FROM Functions WHERE Id IN ('PRODUCT','ORDER','USER','REPORT','CONFIGURATION')");
        migrationBuilder.Sql("DELETE FROM Actions WHERE Id IN ('VIEW','CREATE','UPDATE','DELETE','APPROVE','EXPORT')");
    }
}
