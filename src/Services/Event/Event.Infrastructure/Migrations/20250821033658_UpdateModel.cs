using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventAggregate_ETag",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_EventAggregate_ETagTimestamp",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAggregate_ETag_NotEmpty",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAggregate_ETag_ValidTimestamp",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "SeatMap",
                schema: "event",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "ETagUpdatedAt",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "ETagValue",
                schema: "event",
                table: "ticket_types");

            // Note: CustomerEmail column drop from reservations table removed
            // because reservations table is dropped in RemoveReservationTables migration

            migrationBuilder.DropColumn(
                name: "etag_updated_at",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "etag_value",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "event",
                table: "events");

            migrationBuilder.AddColumn<Guid>(
                name: "OrganizationId",
                schema: "event",
                table: "venues",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Note: Status column alter on reservations table removed
            // because reservations table is dropped in RemoveReservationTables migration

            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "event",
                table: "pricing_rules",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RefundCutoffDays",
                schema: "event",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OrganizationId",
                schema: "event",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "event",
                table: "pricing_rules");

            migrationBuilder.DropColumn(
                name: "RefundCutoffDays",
                schema: "event",
                table: "events");

            migrationBuilder.AddColumn<string>(
                name: "SeatMap",
                schema: "event",
                table: "venues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ETagUpdatedAt",
                schema: "event",
                table: "ticket_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ETagValue",
                schema: "event",
                table: "ticket_types",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Note: Status column alter on reservations table removed
            // because reservations table is dropped in RemoveReservationTables migration

            // Note: CustomerEmail column add to reservations table removed
            // because reservations table is dropped in RemoveReservationTables migration

            migrationBuilder.AddColumn<DateTime>(
                name: "etag_updated_at",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "etag_value",
                schema: "event",
                table: "events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "event",
                table: "events",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventAggregate_ETag",
                schema: "event",
                table: "events",
                column: "etag_value");

            migrationBuilder.CreateIndex(
                name: "IX_EventAggregate_ETagTimestamp",
                schema: "event",
                table: "events",
                column: "etag_updated_at");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAggregate_ETag_NotEmpty",
                schema: "event",
                table: "events",
                sql: "LENGTH(etag_value) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAggregate_ETag_ValidTimestamp",
                schema: "event",
                table: "events",
                sql: "etag_updated_at <= NOW() AND etag_updated_at > '2024-01-01'::timestamp");
        }
    }
}
