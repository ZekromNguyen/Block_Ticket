using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ticketing.Infrastructure.Persistence;

#nullable disable

namespace Ticketing.Infrastructure.Migrations;

[DbContext(typeof(TicketingDbContext))]
[Migration("20260615143000_P0TicketingWorkflow")]
public partial class P0TicketingWorkflow : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF to_regclass('public."TicketTransactions"') IS NOT NULL AND to_regclass('public."LegacyTicketTransactions"') IS NULL THEN
                    ALTER TABLE "TicketTransactions" RENAME TO "LegacyTicketTransactions";
                ELSIF to_regclass('public."TicketTransactions"') IS NOT NULL THEN
                    DROP TABLE "TicketTransactions";
                END IF;

                IF to_regclass('public."Tickets"') IS NOT NULL AND to_regclass('public."LegacyTickets"') IS NULL THEN
                    ALTER TABLE "Tickets" RENAME TO "LegacyTickets";
                ELSIF to_regclass('public."Tickets"') IS NOT NULL THEN
                    DROP TABLE "Tickets";
                END IF;
            END $$;
            """);

        migrationBuilder.CreateTable(
            name: "Reservations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ServiceFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                ProcessingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                InventoryLockOwner = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PaymentIntentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                FailureReason = table.Column<string>(type: "text", nullable: true),
                ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Reservations", x => x.Id));

        migrationBuilder.CreateTable(
            name: "ReservationItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Quantity = table.Column<int>(type: "integer", nullable: false),
                TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
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
                table.PrimaryKey("PK_ReservationItems", x => x.Id);
                table.ForeignKey("FK_ReservationItems_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ReservationPayments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                PaymentIntentId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                FailureReason = table.Column<string>(type: "text", nullable: true),
                TransactionId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                ProcessorData = table.Column<string>(type: "text", nullable: false),
                ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                table.PrimaryKey("PK_ReservationPayments", x => x.Id);
                table.ForeignKey("FK_ReservationPayments_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Tickets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TicketNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                ReservationItemId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                PricePaid = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                ContractAddress = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                TokenId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                TransactionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                MintedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                VerificationCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                UsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                UsedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                UsedLocation = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                table.PrimaryKey("PK_Tickets", x => x.Id);
                table.ForeignKey("FK_Tickets_Reservations_ReservationId", x => x.ReservationId, "Reservations", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Reservations_IdempotencyKey", "Reservations", "IdempotencyKey", unique: true, filter: "\"IdempotencyKey\" IS NOT NULL");
        migrationBuilder.CreateIndex("IX_Reservations_ReservationNumber", "Reservations", "ReservationNumber", unique: true);
        migrationBuilder.CreateIndex("IX_ReservationItems_ReservationId_TicketTypeId", "ReservationItems", new[] { "ReservationId", "TicketTypeId" });
        migrationBuilder.CreateIndex("IX_ReservationPayments_PaymentIntentId", "ReservationPayments", "PaymentIntentId");
        migrationBuilder.CreateIndex("IX_ReservationPayments_ReservationId", "ReservationPayments", "ReservationId");
        migrationBuilder.CreateIndex("IX_Tickets_ReservationId", "Tickets", "ReservationId");
        migrationBuilder.CreateIndex("IX_Tickets_TicketNumber", "Tickets", "TicketNumber", unique: true);
        migrationBuilder.CreateIndex("IX_Tickets_UserId_EventId", "Tickets", new[] { "UserId", "EventId" });
        migrationBuilder.CreateIndex("IX_Tickets_VerificationCode", "Tickets", "VerificationCode", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ReservationItems");
        migrationBuilder.DropTable("ReservationPayments");
        migrationBuilder.DropTable("Tickets");
        migrationBuilder.DropTable("Reservations");

        migrationBuilder.Sql("""
            DO $$
            BEGIN
                IF to_regclass('public."LegacyTickets"') IS NOT NULL AND to_regclass('public."Tickets"') IS NULL THEN
                    ALTER TABLE "LegacyTickets" RENAME TO "Tickets";
                END IF;

                IF to_regclass('public."LegacyTicketTransactions"') IS NOT NULL AND to_regclass('public."TicketTransactions"') IS NULL THEN
                    ALTER TABLE "LegacyTicketTransactions" RENAME TO "TicketTransactions";
                END IF;
            END $$;
            """);
    }
}
