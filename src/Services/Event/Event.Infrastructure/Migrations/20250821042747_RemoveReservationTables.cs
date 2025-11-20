using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL to drop tables with IF EXISTS to handle cases where tables might not exist
            migrationBuilder.Sql("DROP TABLE IF EXISTS event.allocations;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS event.idempotency_records;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS event.reservation_items;");
            migrationBuilder.Sql("DROP TABLE IF EXISTS event.reservations;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "allocations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    AccessCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AllocatedQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    allocated_seat_ids = table.Column<string>(type: "jsonb", nullable: false),
                    allowed_email_domains = table.Column<string>(type: "jsonb", nullable: true),
                    allowed_user_ids = table.Column<string>(type: "jsonb", nullable: true),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allocations", x => x.Id);
                    table.CheckConstraint("CK_Allocations_AccessCode_Consistency", "(\"Type\" IN ('Presale', 'VIP') AND \"AccessCode\" IS NOT NULL) OR (\"Type\" NOT IN ('Presale', 'VIP'))");
                    table.CheckConstraint("CK_Allocations_AvailabilityWindow_Valid", "\"AvailableFrom\" IS NULL OR \"AvailableUntil\" IS NULL OR \"AvailableFrom\" < \"AvailableUntil\"");
                    table.CheckConstraint("CK_Allocations_ExpiresAt_Valid", "\"ExpiresAt\" IS NULL OR \"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_Allocations_NotExpiredWhenActive", "NOT \"IsActive\" OR \"ExpiresAt\" IS NULL OR \"ExpiresAt\" > NOW()");
                    table.CheckConstraint("CK_Allocations_Quantity_Valid", "\"TotalQuantity\" > 0 AND \"AllocatedQuantity\" >= 0 AND \"AllocatedQuantity\" <= \"TotalQuantity\"");
                    table.CheckConstraint("CK_Allocations_Type_Valid", "\"Type\" IN ('Public', 'PromoterHold', 'ArtistHold', 'Presale', 'VIP', 'Press')");
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

            migrationBuilder.CreateTable(
                name: "idempotency_records",
                schema: "event",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    http_method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: true),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    request_body = table.Column<string>(type: "text", nullable: true),
                    request_headers = table.Column<string>(type: "jsonb", nullable: true),
                    request_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    request_path = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    response_body = table.Column<string>(type: "text", nullable: true),
                    response_headers = table.Column<string>(type: "jsonb", nullable: true),
                    response_status_code = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.id);
                    table.CheckConstraint("ck_idempotency_records_expires_at_future", "expires_at > processed_at");
                    table.CheckConstraint("ck_idempotency_records_http_method", "http_method IN ('GET', 'POST', 'PUT', 'PATCH', 'DELETE')");
                    table.CheckConstraint("ck_idempotency_records_status_code", "response_status_code >= 0 AND response_status_code <= 999");
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<string>(type: "text", nullable: true),
                    DiscountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ReservationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    discount_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    discount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.Id);
                    table.CheckConstraint("CK_Reservations_CancelledAt_Valid", "\"CancelledAt\" IS NULL OR \"CancelledAt\" >= \"CreatedAt\"");
                    table.CheckConstraint("CK_Reservations_ConfirmedAt_Valid", "\"ConfirmedAt\" IS NULL OR \"ConfirmedAt\" >= \"CreatedAt\"");
                    table.CheckConstraint("CK_Reservations_Currency_Match", "discount_currency IS NULL OR discount_currency = currency");
                    table.CheckConstraint("CK_Reservations_DiscountAmount_Positive", "discount_amount IS NULL OR discount_amount >= 0");
                    table.CheckConstraint("CK_Reservations_ExpiresAt_Future", "\"ExpiresAt\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_Reservations_Status_Valid", "\"Status\" IN ('Active', 'Confirmed', 'Cancelled', 'Expired', 'Released')");
                    table.CheckConstraint("CK_Reservations_Timing_Valid", "(\"Status\" = 'Confirmed' AND \"ConfirmedAt\" IS NOT NULL) OR (\"Status\" = 'Cancelled' AND \"CancelledAt\" IS NOT NULL) OR (\"Status\" IN ('Active', 'Expired', 'Released'))");
                    table.CheckConstraint("CK_Reservations_TotalAmount_Positive", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_reservations_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_items",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_items", x => x.Id);
                    table.CheckConstraint("CK_ReservationItems_Quantity_Positive", "\"Quantity\" > 0");
                    table.CheckConstraint("CK_ReservationItems_UnitPrice_Positive", "unit_price_amount >= 0");
                    table.ForeignKey(
                        name: "FK_reservation_items_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalSchema: "event",
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_AccessCode_Active",
                schema: "event",
                table: "allocations",
                columns: new[] { "AccessCode", "IsActive" },
                filter: "\"AccessCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Active_Expires",
                schema: "event",
                table: "allocations",
                columns: new[] { "IsActive", "ExpiresAt" },
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_AvailabilityWindow",
                schema: "event",
                table: "allocations",
                columns: new[] { "AvailableFrom", "AvailableUntil" },
                filter: "\"AvailableFrom\" IS NOT NULL OR \"AvailableUntil\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Active_Available",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "IsActive", "AvailableFrom", "AvailableUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_Event_Type_Active",
                schema: "event",
                table: "allocations",
                columns: new[] { "EventId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Allocations_TicketTypeId",
                schema: "event",
                table: "allocations",
                column: "TicketTypeId",
                filter: "\"TicketTypeId\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_reservation_items_ReservationId",
                schema: "event",
                table: "reservation_items",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationItems_SeatId",
                schema: "event",
                table: "reservation_items",
                column: "SeatId",
                filter: "\"SeatId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ReservationItems_TicketTypeId",
                schema: "event",
                table: "reservation_items",
                column: "TicketTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DiscountCode_Status",
                schema: "event",
                table: "reservations",
                columns: new[] { "DiscountCode", "Status" },
                filter: "\"DiscountCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Event_Confirmed",
                schema: "event",
                table: "reservations",
                columns: new[] { "EventId", "ConfirmedAt" },
                filter: "\"ConfirmedAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Event_Status_Expires",
                schema: "event",
                table: "reservations",
                columns: new[] { "EventId", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_EventId",
                schema: "event",
                table: "reservations",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Number",
                schema: "event",
                table: "reservations",
                column: "ReservationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_Status_Expires",
                schema: "event",
                table: "reservations",
                columns: new[] { "Status", "ExpiresAt" },
                filter: "\"Status\" = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_User_Status",
                schema: "event",
                table: "reservations",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                schema: "event",
                table: "reservations",
                column: "UserId");
        }
    }
}
