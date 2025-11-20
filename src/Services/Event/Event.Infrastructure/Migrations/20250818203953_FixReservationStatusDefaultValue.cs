using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixReservationStatusDefaultValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Note: Reservation status default value fix removed
            // because reservations table is dropped in RemoveReservationTables migration
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Reservation status default value rollback removed
            // because reservations table is dropped in RemoveReservationTables migration
        }
    }
}
