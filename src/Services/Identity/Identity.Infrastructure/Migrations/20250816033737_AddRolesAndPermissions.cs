using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Permissions_Roles_RoleId",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_CreatedAt",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Resource",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Resource_Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_RoleId",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_RoleId_Resource_Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RoleId",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "identity",
                table: "Roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                schema: "identity",
                table: "Permissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "identity",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                schema: "identity",
                table: "Permissions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeletedBy",
                schema: "identity",
                table: "Permissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                schema: "identity",
                table: "Permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "identity",
                table: "Permissions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "identity",
                table: "Permissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Service",
                schema: "identity",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    RoleId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId1",
                        column: x => x.RoleId1,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                schema: "identity",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Service_Resource_Action",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Service", "Resource", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_ExpiresAt",
                schema: "identity",
                table: "RolePermissions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_IsActive",
                schema: "identity",
                table: "RolePermissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId",
                schema: "identity",
                table: "RolePermissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RoleId1",
                schema: "identity",
                table: "RolePermissions",
                column: "RoleId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Name",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Service_Resource_Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Description",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "Service",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.AlterColumn<string>(
                name: "Scope",
                schema: "identity",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Action",
                schema: "identity",
                table: "Permissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<Guid>(
                name: "RoleId",
                schema: "identity",
                table: "Permissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Action",
                schema: "identity",
                table: "Permissions",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_CreatedAt",
                schema: "identity",
                table: "Permissions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource",
                schema: "identity",
                table: "Permissions",
                column: "Resource");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Resource", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleId",
                schema: "identity",
                table: "Permissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_RoleId_Resource_Action",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "RoleId", "Resource", "Action" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Permissions_Roles_RoleId",
                schema: "identity",
                table: "Permissions",
                column: "RoleId",
                principalSchema: "identity",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
