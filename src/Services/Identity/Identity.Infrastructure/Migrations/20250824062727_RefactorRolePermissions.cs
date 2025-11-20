using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorRolePermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordHistory_Users_UserId",
                schema: "identity",
                table: "PasswordHistory");

            migrationBuilder.DropIndex(
                name: "IX_Roles_NormalizedName",
                schema: "identity",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_ExpiresAt",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_IsActive",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "identity",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_IsActive",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Service_Resource_Action",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PasswordHistory",
                schema: "identity",
                table: "PasswordHistory");

            migrationBuilder.DropIndex(
                name: "IX_PasswordHistory_UserId_CreatedAt",
                schema: "identity",
                table: "PasswordHistory");

            migrationBuilder.DropIndex(
                name: "IX_PasswordHistory_UserId_PasswordHash",
                schema: "identity",
                table: "PasswordHistory");

            migrationBuilder.DropColumn(
                name: "NormalizedName",
                schema: "identity",
                table: "Roles");

            migrationBuilder.RenameTable(
                name: "PasswordHistory",
                schema: "identity",
                newName: "PasswordHistories",
                newSchema: "identity");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordHistory_UserId",
                schema: "identity",
                table: "PasswordHistories",
                newName: "IX_PasswordHistories_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                schema: "identity",
                table: "Permissions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

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
                name: "Name",
                schema: "identity",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "identity",
                table: "Permissions",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

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

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "identity",
                table: "PasswordHistories",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "identity",
                table: "PasswordHistories",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PasswordHistories",
                schema: "identity",
                table: "PasswordHistories",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Resource_Action_Service",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Resource", "Action", "Service" });

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordHistories_Users_UserId",
                schema: "identity",
                table: "PasswordHistories",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordHistories_Users_UserId",
                schema: "identity",
                table: "PasswordHistories");

            migrationBuilder.DropIndex(
                name: "IX_Permissions_Resource_Action_Service",
                schema: "identity",
                table: "Permissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PasswordHistories",
                schema: "identity",
                table: "PasswordHistories");

            migrationBuilder.RenameTable(
                name: "PasswordHistories",
                schema: "identity",
                newName: "PasswordHistory",
                newSchema: "identity");

            migrationBuilder.RenameIndex(
                name: "IX_PasswordHistories_UserId",
                schema: "identity",
                table: "PasswordHistory",
                newName: "IX_PasswordHistory_UserId");

            migrationBuilder.AddColumn<string>(
                name: "NormalizedName",
                schema: "identity",
                table: "Roles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "Service",
                schema: "identity",
                table: "Permissions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

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
                name: "Name",
                schema: "identity",
                table: "Permissions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                schema: "identity",
                table: "Permissions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

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

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                schema: "identity",
                table: "PasswordHistory",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<Guid>(
                name: "Id",
                schema: "identity",
                table: "PasswordHistory",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PasswordHistory",
                schema: "identity",
                table: "PasswordHistory",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_NormalizedName",
                schema: "identity",
                table: "Roles",
                column: "NormalizedName",
                unique: true);

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
                name: "IX_RolePermissions_RoleId_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_IsActive",
                schema: "identity",
                table: "Permissions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Service_Resource_Action",
                schema: "identity",
                table: "Permissions",
                columns: new[] { "Service", "Resource", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_UserId_CreatedAt",
                schema: "identity",
                table: "PasswordHistory",
                columns: new[] { "UserId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordHistory_UserId_PasswordHash",
                schema: "identity",
                table: "PasswordHistory",
                columns: new[] { "UserId", "PasswordHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordHistory_Users_UserId",
                schema: "identity",
                table: "PasswordHistory",
                column: "UserId",
                principalSchema: "identity",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
