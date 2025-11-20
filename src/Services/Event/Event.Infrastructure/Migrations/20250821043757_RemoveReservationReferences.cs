using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservationReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Seats_CurrentReservationId",
                schema: "event",
                table: "seats");

            migrationBuilder.DropColumn(
                name: "CurrentReservationId",
                schema: "event",
                table: "seats");
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

            migrationBuilder.CreateIndex(
                name: "IX_Seats_CurrentReservationId",
                schema: "event",
                table: "seats",
                column: "CurrentReservationId",
                filter: "\"CurrentReservationId\" IS NOT NULL");
        }
    }
}
