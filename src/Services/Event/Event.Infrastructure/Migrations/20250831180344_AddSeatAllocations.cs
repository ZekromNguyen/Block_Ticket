using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSeatAllocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Venues_HasSeatMap",
                schema: "event",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_Name",
                schema: "event",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_SeatMapLastUpdated",
                schema: "event",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_Venues_TotalCapacity",
                schema: "event",
                table: "venues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Venues_Capacity_Positive",
                schema: "event",
                table: "venues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Venues_Coordinates_Valid",
                schema: "event",
                table: "venues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Venues_Email_Format",
                schema: "event",
                table: "venues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Venues_SeatMap_Consistency",
                schema: "event",
                table: "venues");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Venues_Website_Format",
                schema: "event",
                table: "venues");

            migrationBuilder.DropIndex(
                name: "IX_Events_ActiveEvents",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_Events_SearchVector",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_EventDate_Future",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_PublishWindow_Valid",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Status_Valid",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Version_Positive",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_Active_Dates",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_Active_MaxEvents",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_Name",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_Organization_Active",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_Organization_Slug",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropIndex(
                name: "IX_EventSeries_PromoterId",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSeries_MaxEvents_Positive",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSeries_SeriesDates_Valid",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSeries_SeriesStartDate_Valid",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventSeries_Version_Positive",
                schema: "event",
                table: "event_series");

            migrationBuilder.DropColumn(
                name: "SearchVector",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "event",
                table: "events");

            migrationBuilder.AlterColumn<string>(
                name: "usage_contexts",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "tags",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "quality_urls",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "metadata_properties",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "keywords",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb");

            migrationBuilder.AlterColumn<string>(
                name: "compliance_warnings",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "compliance_violations",
                schema: "event",
                table: "marketing_assets",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                schema: "event",
                table: "allocations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ByQuantity");

            migrationBuilder.CreateTable(
                name: "Organizations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Organizations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Promoters",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_Promoters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeatAllocations",
                schema: "event",
                columns: table => new
                {
                    AllocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeatAllocations", x => new { x.AllocationId, x.SeatId });
                    table.ForeignKey(
                        name: "FK_SeatAllocations_allocations_AllocationId",
                        column: x => x.AllocationId,
                        principalSchema: "event",
                        principalTable: "allocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeatAllocations_seats_SeatId",
                        column: x => x.SeatId,
                        principalSchema: "event",
                        principalTable: "seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeatAllocations_SeatId",
                schema: "event",
                table: "SeatAllocations",
                column: "SeatId");

            migrationBuilder.AddForeignKey(
                name: "FK_events_Organizations_OrganizationId",
                schema: "event",
                table: "events",
                column: "OrganizationId",
                principalSchema: "event",
                principalTable: "Organizations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_events_Promoters_PromoterId",
                schema: "event",
                table: "events",
                column: "PromoterId",
                principalSchema: "event",
                principalTable: "Promoters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_Organizations_OrganizationId",
                schema: "event",
                table: "events");

            migrationBuilder.DropForeignKey(
                name: "FK_events_Promoters_PromoterId",
                schema: "event",
                table: "events");

            migrationBuilder.DropTable(
                name: "Organizations",
                schema: "event");

            migrationBuilder.DropTable(
                name: "Promoters",
                schema: "event");

            migrationBuilder.DropTable(
                name: "SeatAllocations",
                schema: "event");

            migrationBuilder.DropColumn(
                name: "Scope",
                schema: "event",
                table: "allocations");

            migrationBuilder.AlterColumn<string>(
                name: "usage_contexts",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "tags",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "quality_urls",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "metadata_properties",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "keywords",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "compliance_warnings",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "compliance_violations",
                schema: "event",
                table: "marketing_assets",
                type: "jsonb",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "event",
                table: "events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "SearchVector",
                schema: "event",
                table: "events",
                type: "tsvector",
                nullable: false,
                computedColumnSql: "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(description, ''))",
                stored: true)
                .Annotation("Npgsql:TsVectorConfig", "english")
                .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" });

            migrationBuilder.CreateIndex(
                name: "IX_Venues_HasSeatMap",
                schema: "event",
                table: "venues",
                column: "HasSeatMap");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_Name",
                schema: "event",
                table: "venues",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_SeatMapLastUpdated",
                schema: "event",
                table: "venues",
                column: "SeatMapLastUpdated",
                filter: "\"SeatMapLastUpdated\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Venues_TotalCapacity",
                schema: "event",
                table: "venues",
                column: "TotalCapacity");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venues_Capacity_Positive",
                schema: "event",
                table: "venues",
                sql: "\"TotalCapacity\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venues_Coordinates_Valid",
                schema: "event",
                table: "venues",
                sql: "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude >= -90 AND latitude <= 90 AND longitude >= -180 AND longitude <= 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venues_Email_Format",
                schema: "event",
                table: "venues",
                sql: "\"ContactEmail\" IS NULL OR \"ContactEmail\" ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venues_SeatMap_Consistency",
                schema: "event",
                table: "venues",
                sql: "(\"HasSeatMap\" = false AND \"SeatMapMetadata\" IS NULL AND \"SeatMapChecksum\" IS NULL AND \"SeatMapLastUpdated\" IS NULL) OR (\"HasSeatMap\" = true AND \"SeatMapMetadata\" IS NOT NULL AND \"SeatMapChecksum\" IS NOT NULL AND \"SeatMapLastUpdated\" IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Venues_Website_Format",
                schema: "event",
                table: "venues",
                sql: "\"Website\" IS NULL OR \"Website\" ~ '^https?://'");

            migrationBuilder.CreateIndex(
                name: "IX_Events_ActiveEvents",
                schema: "event",
                table: "events",
                columns: new[] { "EventDate", "Status" },
                filter: "\"Status\" IN ('Published', 'OnSale')");

            migrationBuilder.CreateIndex(
                name: "IX_Events_SearchVector",
                schema: "event",
                table: "events",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_EventDate_Future",
                schema: "event",
                table: "events",
                sql: "\"EventDate\" > \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_PublishWindow_Valid",
                schema: "event",
                table: "events",
                sql: "publish_start_date IS NULL OR publish_end_date IS NULL OR publish_start_date < publish_end_date");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Status_Valid",
                schema: "event",
                table: "events",
                sql: "\"Status\" IN ('Draft', 'Review', 'Published', 'OnSale', 'SoldOut', 'Completed', 'Cancelled', 'Archived')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Version_Positive",
                schema: "event",
                table: "events",
                sql: "\"Version\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_Active_Dates",
                schema: "event",
                table: "event_series",
                columns: new[] { "IsActive", "SeriesStartDate", "SeriesEndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_Active_MaxEvents",
                schema: "event",
                table: "event_series",
                columns: new[] { "IsActive", "MaxEvents" },
                filter: "\"MaxEvents\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_Name",
                schema: "event",
                table: "event_series",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_Organization_Active",
                schema: "event",
                table: "event_series",
                columns: new[] { "OrganizationId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_Organization_Slug",
                schema: "event",
                table: "event_series",
                columns: new[] { "OrganizationId", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventSeries_PromoterId",
                schema: "event",
                table: "event_series",
                column: "PromoterId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSeries_MaxEvents_Positive",
                schema: "event",
                table: "event_series",
                sql: "\"MaxEvents\" IS NULL OR \"MaxEvents\" > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSeries_SeriesDates_Valid",
                schema: "event",
                table: "event_series",
                sql: "\"SeriesStartDate\" IS NULL OR \"SeriesEndDate\" IS NULL OR \"SeriesStartDate\" < \"SeriesEndDate\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSeries_SeriesStartDate_Valid",
                schema: "event",
                table: "event_series",
                sql: "\"SeriesStartDate\" IS NULL OR \"SeriesStartDate\" >= \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventSeries_Version_Positive",
                schema: "event",
                table: "event_series",
                sql: "\"Version\" > 0");
        }
    }
}
