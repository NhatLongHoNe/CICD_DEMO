using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DemoCICD.Persistence.Migrations;

/// <inheritdoc />
public partial class AddRefreshToken : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "RefreshToken",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                ReplacedByToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RefreshToken", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RefreshToken_Token",
            table: "RefreshToken",
            column: "Token");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "RefreshToken");
    }
}
