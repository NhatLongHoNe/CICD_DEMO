using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoCICD.Persistence.Migrations;

/// <summary>
/// Index trên Permissions(RoleId) để tối ưu GetPermissionsByRoleIdsAsync.
/// </summary>
public partial class AddIndexPermissionsRoleId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_Permissions_RoleId",
            table: "Permissions",
            column: "RoleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Permissions_RoleId",
            table: "Permissions");
    }
}
