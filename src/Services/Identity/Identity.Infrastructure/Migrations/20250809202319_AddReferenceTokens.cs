using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReferenceTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReferenceTokens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TokenType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SessionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsRevoked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RevokedReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Claims = table.Column<string>(type: "jsonb", nullable: true),
                    Scopes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClientId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReferenceTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_ExpiresAt",
                table: "ReferenceTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_SessionId",
                table: "ReferenceTokens",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_TokenId",
                table: "ReferenceTokens",
                column: "TokenId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_TokenType_IsRevoked",
                table: "ReferenceTokens",
                columns: new[] { "TokenType", "IsRevoked" });

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_UserId",
                table: "ReferenceTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReferenceTokens_UserId_TokenType_IsRevoked",
                table: "ReferenceTokens",
                columns: new[] { "UserId", "TokenType", "IsRevoked" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReferenceTokens");
        }
    }
}
