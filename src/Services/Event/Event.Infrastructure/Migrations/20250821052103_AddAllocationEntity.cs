using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAllocationEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "allocations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    AccessCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllowedCustomerSegments = table.Column<string>(type: "jsonb", nullable: false),
                    StartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxPerCustomer = table.Column<int>(type: "integer", nullable: true),
                    MinPerCustomer = table.Column<int>(type: "integer", nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allocations", x => x.Id);
                    table.CheckConstraint("CK_Allocations_MaxPerCustomer_Positive", "\"MaxPerCustomer\" IS NULL OR \"MaxPerCustomer\" > 0");
                    table.CheckConstraint("CK_Allocations_MinMax_PerCustomer", "\"MinPerCustomer\" IS NULL OR \"MaxPerCustomer\" IS NULL OR \"MinPerCustomer\" <= \"MaxPerCustomer\"");
                    table.CheckConstraint("CK_Allocations_MinPerCustomer_Positive", "\"MinPerCustomer\" IS NULL OR \"MinPerCustomer\" > 0");
                    table.CheckConstraint("CK_Allocations_Name_NotEmpty", "LENGTH(TRIM(\"Name\")) > 0");
                    table.CheckConstraint("CK_Allocations_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_Allocations_TimeWindow", "\"StartTime\" IS NULL OR \"EndTime\" IS NULL OR \"StartTime\" < \"EndTime\"");
                    table.CheckConstraint("CK_Allocations_UsedQuantity_LessOrEqual_Quantity", "\"UsedQuantity\" <= \"Quantity\"");
                    table.CheckConstraint("CK_Allocations_UsedQuantity_NonNegative", "\"UsedQuantity\" >= 0");
                    table.ForeignKey(
                        name: "FK_allocations_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_allocations_ticket_types_TicketTypeId",
                        column: x => x.TicketTypeId,
                        principalSchema: "event",
                        principalTable: "ticket_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_AccessCode",
                schema: "event",
                table: "allocations",
                column: "AccessCode",
                unique: true,
                filter: "\"AccessCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Active",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "IsEnabled", "StartTime", "EndTime" },
                filter: "\"IsEnabled\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Enabled",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Priority_Name",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "Priority", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_TimeWindow",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Type",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_EventId",
                schema: "event",
                table: "allocations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_TicketTypeId",
                schema: "event",
                table: "allocations",
                column: "TicketTypeId",
                filter: "\"TicketTypeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Type",
                schema: "event",
                table: "allocations",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocations",
                schema: "event");
        }
    }
}
