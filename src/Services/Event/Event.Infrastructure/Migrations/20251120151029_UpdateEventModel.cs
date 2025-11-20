using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketTypes_Available",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "available_capacity",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.AddColumn<int>(
                name: "held_capacity",
                schema: "event",
                table: "ticket_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "sold_capacity",
                schema: "event",
                table: "ticket_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_Available",
                schema: "event",
                table: "ticket_types",
                columns: new[] { "EventId", "IsVisible" },
                filter: "(total_capacity - held_capacity - sold_capacity) > 0 AND \"IsVisible\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TicketTypes_Available",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "held_capacity",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "sold_capacity",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.AddColumn<int>(
                name: "available_capacity",
                schema: "event",
                table: "ticket_types",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_Available",
                schema: "event",
                table: "ticket_types",
                columns: new[] { "EventId", "IsVisible" },
                filter: "available_capacity > 0 AND \"IsVisible\" = true");
        }
    }
}
