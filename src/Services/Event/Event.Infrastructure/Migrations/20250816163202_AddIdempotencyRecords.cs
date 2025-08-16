using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    request_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    request_body = table.Column<string>(type: "text", nullable: true),
                    request_headers = table.Column<string>(type: "jsonb", nullable: true),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    response_status_code = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    response_headers = table.Column<string>(type: "jsonb", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_idempotency_records_expires_at_future", "expires_at > processed_at");
                    table.CheckConstraint("ck_idempotency_records_http_method", "http_method IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE')");
                    table.CheckConstraint("ck_idempotency_records_status_code", "response_status_code >= 0 AND response_status_code <= 999");
                });

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_expires_at",
                schema: "event",
                table: "idempotency_records",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_key",
                schema: "event",
                table: "idempotency_records",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_organization_id",
                schema: "event",
                table: "idempotency_records",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_path_method",
                schema: "event",
                table: "idempotency_records",
                columns: new[] { "request_path", "http_method" });

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_processed_at",
                schema: "event",
                table: "idempotency_records",
                column: "processed_at");

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_records_user_id",
                schema: "event",
                table: "idempotency_records",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records",
                schema: "event");
        }
    }
}
