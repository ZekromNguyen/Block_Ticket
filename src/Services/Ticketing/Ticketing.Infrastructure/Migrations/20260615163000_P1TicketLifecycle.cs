using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Ticketing.Infrastructure.Persistence;

#nullable disable

namespace Ticketing.Infrastructure.Migrations;

[DbContext(typeof(TicketingDbContext))]
[Migration("20260615163000_P1TicketLifecycle")]
public partial class P1TicketLifecycle : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsResaleEligible",
            table: "Tickets",
            type: "boolean",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "ListedForResaleAt",
            table: "Tickets",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "RefundedAmount",
            table: "Tickets",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "RefundedAt",
            table: "Tickets",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "RefundReason",
            table: "Tickets",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ResalePrice",
            table: "Tickets",
            type: "decimal(18,2)",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "ResaleSellerUserId",
            table: "Tickets",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "TransferredAt",
            table: "Tickets",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "TransferredFromUserId",
            table: "Tickets",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<bool>(
            name: "VerificationOverrideAllowed",
            table: "Tickets",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.AddColumn<string>(
            name: "VerificationOverrideReason",
            table: "Tickets",
            type: "character varying(300)",
            maxLength: 300,
            nullable: true);

        migrationBuilder.AddColumn<DateTime>(
            name: "VerificationOverrideUntil",
            table: "Tickets",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "AdminAuditNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TicketId = table.Column<Guid>(type: "uuid", nullable: true),
                ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                Action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                AdminUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                Note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_AdminAuditNotes", x => x.Id));

        migrationBuilder.CreateTable(
            name: "WaitingListEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                EventId = table.Column<Guid>(type: "uuid", nullable: false),
                TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                OfferedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                OfferExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedBy = table.Column<string>(type: "text", nullable: true),
                UpdatedBy = table.Column<string>(type: "text", nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<string>(type: "text", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_WaitingListEntries", x => x.Id));

        migrationBuilder.CreateIndex("IX_Tickets_ResaleSellerUserId", "Tickets", "ResaleSellerUserId");
        migrationBuilder.CreateIndex("IX_Tickets_Status_EventId", "Tickets", new[] { "Status", "EventId" });
        migrationBuilder.CreateIndex("IX_AdminAuditNotes_ReservationId", "AdminAuditNotes", "ReservationId");
        migrationBuilder.CreateIndex("IX_AdminAuditNotes_TicketId", "AdminAuditNotes", "TicketId");
        migrationBuilder.CreateIndex("IX_WaitingListEntries_EventId_TicketTypeId_Status_JoinedAt", "WaitingListEntries", new[] { "EventId", "TicketTypeId", "Status", "JoinedAt" });
        migrationBuilder.CreateIndex("IX_WaitingListEntries_EventId_TicketTypeId_UserId", "WaitingListEntries", new[] { "EventId", "TicketTypeId", "UserId" }, unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("AdminAuditNotes");
        migrationBuilder.DropTable("WaitingListEntries");
        migrationBuilder.DropIndex("IX_Tickets_ResaleSellerUserId", "Tickets");
        migrationBuilder.DropIndex("IX_Tickets_Status_EventId", "Tickets");
        migrationBuilder.DropColumn("IsResaleEligible", "Tickets");
        migrationBuilder.DropColumn("ListedForResaleAt", "Tickets");
        migrationBuilder.DropColumn("RefundedAmount", "Tickets");
        migrationBuilder.DropColumn("RefundedAt", "Tickets");
        migrationBuilder.DropColumn("RefundReason", "Tickets");
        migrationBuilder.DropColumn("ResalePrice", "Tickets");
        migrationBuilder.DropColumn("ResaleSellerUserId", "Tickets");
        migrationBuilder.DropColumn("TransferredAt", "Tickets");
        migrationBuilder.DropColumn("TransferredFromUserId", "Tickets");
        migrationBuilder.DropColumn("VerificationOverrideAllowed", "Tickets");
        migrationBuilder.DropColumn("VerificationOverrideReason", "Tickets");
        migrationBuilder.DropColumn("VerificationOverrideUntil", "Tickets");
    }
}
