using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservationPropertiesFromSeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to drop indexes and columns with IF EXISTS to handle cases where they might not exist
            migrationBuilder.Sql("DROP INDEX IF EXISTS event.\"IX_Seats_CurrentReservationId\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS event.\"IX_Seats_Status_ReservedUntil\";");

            // Drop columns if they exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'event' AND table_name = 'seats' AND column_name = 'CurrentReservationId') THEN
                        ALTER TABLE event.seats DROP COLUMN ""CurrentReservationId"";
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'event' AND table_name = 'seats' AND column_name = 'ReservedUntil') THEN
                        ALTER TABLE event.seats DROP COLUMN ""ReservedUntil"";
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentReservationId",
                schema: "event",
                table: "seats",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUntil",
                schema: "event",
                table: "seats",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seats_CurrentReservationId",
                schema: "event",
                table: "seats",
                column: "CurrentReservationId",
                filter: "\"CurrentReservationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Status_ReservedUntil",
                schema: "event",
                table: "seats",
                columns: new[] { "Status", "ReservedUntil" },
                filter: "\"ReservedUntil\" IS NOT NULL");
        }
    }
}
