using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "event");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserEmail = table.Column<string>(type: "text", nullable: true),
                    ActorType = table.Column<string>(type: "text", nullable: true),
                    ActorIdentifier = table.Column<string>(type: "text", nullable: true),
                    RequestId = table.Column<string>(type: "text", nullable: true),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    IpAddress = table.Column<string>(type: "text", nullable: true),
                    UserAgent = table.Column<string>(type: "text", nullable: true),
                    OldValues = table.Column<string>(type: "jsonb", nullable: true),
                    NewValues = table.Column<string>(type: "jsonb", nullable: true),
                    ChangedProperties = table.Column<string>(type: "text", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    AdditionalData = table.Column<string>(type: "jsonb", nullable: true),
                    Source = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "event_series",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeriesStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SeriesEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxEvents = table.Column<int>(type: "integer", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    event_ids = table.Column<string>(type: "jsonb", nullable: false),
                    categories = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_event_series", x => x.Id);
                    table.CheckConstraint("CK_EventSeries_MaxEvents_Positive", "\"MaxEvents\" IS NULL OR \"MaxEvents\" > 0");
                    table.CheckConstraint("CK_EventSeries_SeriesDates_Valid", "\"SeriesStartDate\" IS NULL OR \"SeriesEndDate\" IS NULL OR \"SeriesStartDate\" < \"SeriesEndDate\"");
                    table.CheckConstraint("CK_EventSeries_SeriesStartDate_Valid", "\"SeriesStartDate\" IS NULL OR \"SeriesStartDate\" >= \"CreatedAt\"");
                    table.CheckConstraint("CK_EventSeries_Version_Positive", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "events",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromoterId = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    EventDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    publish_start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    publish_end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    publish_time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    BannerUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SeoTitle = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SeoDescription = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ChangeHistory = table.Column<string>(type: "jsonb", nullable: true),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false, computedColumnSql: "to_tsvector('english', coalesce(title, '') || ' ' || coalesce(description, ''))", stored: true)
                        .Annotation("Npgsql:TsVectorConfig", "english")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "Title", "Description" }),
                    categories = table.Column<string>(type: "jsonb", nullable: false),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_events", x => x.Id);
                    table.CheckConstraint("CK_Events_EventDate_Future", "\"EventDate\" > \"CreatedAt\"");
                    table.CheckConstraint("CK_Events_PublishWindow_Valid", "publish_start_date IS NULL OR publish_end_date IS NULL OR publish_start_date < publish_end_date");
                    table.CheckConstraint("CK_Events_Status_Valid", "\"Status\" IN ('Draft', 'Review', 'Published', 'OnSale', 'SoldOut', 'Completed', 'Cancelled', 'Archived')");
                    table.CheckConstraint("CK_Events_Version_Positive", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "venues",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<double>(type: "numeric(10,8)", nullable: true),
                    longitude = table.Column<double>(type: "numeric(11,8)", nullable: true),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TotalCapacity = table.Column<int>(type: "integer", nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContactPhone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Website = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    HasSeatMap = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SeatMapMetadata = table.Column<string>(type: "jsonb", nullable: true),
                    SeatMapChecksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    SeatMapLastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_venues", x => x.Id);
                    table.CheckConstraint("CK_Venues_Capacity_Positive", "\"TotalCapacity\" > 0");
                    table.CheckConstraint("CK_Venues_Coordinates_Valid", "(latitude IS NULL AND longitude IS NULL) OR (latitude IS NOT NULL AND longitude IS NOT NULL AND latitude >= -90 AND latitude <= 90 AND longitude >= -180 AND longitude <= 180)");
                    table.CheckConstraint("CK_Venues_Email_Format", "\"ContactEmail\" IS NULL OR \"ContactEmail\" ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'");
                    table.CheckConstraint("CK_Venues_SeatMap_Consistency", "(\"HasSeatMap\" = false AND \"SeatMapMetadata\" IS NULL AND \"SeatMapChecksum\" IS NULL AND \"SeatMapLastUpdated\" IS NULL) OR (\"HasSeatMap\" = true AND \"SeatMapMetadata\" IS NOT NULL AND \"SeatMapChecksum\" IS NOT NULL AND \"SeatMapLastUpdated\" IS NOT NULL)");
                    table.CheckConstraint("CK_Venues_Website_Format", "\"Website\" IS NULL OR \"Website\" ~ '^https?://'");
                });

            migrationBuilder.CreateTable(
                name: "pricing_rules",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscountType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DiscountValue = table.Column<decimal>(type: "numeric(10,4)", nullable: true),
                    max_discount_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    max_discount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    min_order_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    min_order_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    DiscountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsSingleUse = table.Column<bool>(type: "boolean", nullable: true),
                    MaxUses = table.Column<int>(type: "integer", nullable: true),
                    CurrentUses = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    target_ticket_type_ids = table.Column<string>(type: "jsonb", nullable: true),
                    target_customer_segments = table.Column<string>(type: "jsonb", nullable: true),
                    EventId1 = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_pricing_rules", x => x.Id);
                    table.CheckConstraint("CK_PricingRules_DiscountCode_Consistency", "(\"Type\" = 'DiscountCode' AND \"DiscountCode\" IS NOT NULL) OR (\"Type\" != 'DiscountCode' AND \"DiscountCode\" IS NULL)");
                    table.CheckConstraint("CK_PricingRules_DiscountType_Valid", "\"DiscountType\" IS NULL OR \"DiscountType\" IN ('FixedAmount', 'Percentage')");
                    table.CheckConstraint("CK_PricingRules_DiscountValue_Valid", "\"DiscountValue\" IS NULL OR \"DiscountValue\" >= 0");
                    table.CheckConstraint("CK_PricingRules_EffectivePeriod_Valid", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" > \"EffectiveFrom\"");
                    table.CheckConstraint("CK_PricingRules_MoneyAmounts_Positive", "(max_discount_amount IS NULL OR max_discount_amount >= 0) AND (min_order_amount IS NULL OR min_order_amount >= 0)");
                    table.CheckConstraint("CK_PricingRules_Percentage_Valid", "\"DiscountType\" != 'Percentage' OR (\"DiscountValue\" IS NOT NULL AND \"DiscountValue\" <= 100)");
                    table.CheckConstraint("CK_PricingRules_Priority_Valid", "\"Priority\" >= 0");
                    table.CheckConstraint("CK_PricingRules_Quantity_Valid", "(\"MinQuantity\" IS NULL AND \"MaxQuantity\" IS NULL) OR (\"MinQuantity\" IS NOT NULL AND \"MaxQuantity\" IS NOT NULL AND \"MinQuantity\" <= \"MaxQuantity\" AND \"MinQuantity\" > 0)");
                    table.CheckConstraint("CK_PricingRules_Type_Valid", "\"Type\" IN ('BasePrice', 'TimeBased', 'QuantityBased', 'DiscountCode', 'Dynamic')");
                    table.CheckConstraint("CK_PricingRules_Usage_Valid", "\"CurrentUses\" >= 0 AND (\"MaxUses\" IS NULL OR \"CurrentUses\" <= \"MaxUses\")");
                    table.ForeignKey(
                        name: "FK_pricing_rules_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_pricing_rules_events_EventId1",
                        column: x => x.EventId1,
                        principalSchema: "event",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    DiscountCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    discount_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    discount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    CustomerNotes = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    CancellationReason = table.Column<string>(type: "text", nullable: true),
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
                name: "ticket_types",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    base_price_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    base_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    service_fee_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    service_fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    tax_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                    tax_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    InventoryType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_capacity = table.Column<int>(type: "integer", nullable: false),
                    available_capacity = table.Column<int>(type: "integer", nullable: false),
                    MinPurchaseQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    MaxPurchaseQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    MaxPerCustomer = table.Column<int>(type: "integer", nullable: false, defaultValue: 10),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsResaleAllowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    RequiresApproval = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    on_sale_windows = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_ticket_types", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ticket_types_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    section = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    row = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    seat_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Available"),
                    IsAccessible = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HasRestrictedView = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PriceCategory = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CurrentReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AllocatedToTicketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seats_venues_VenueId",
                        column: x => x.VenueId,
                        principalSchema: "event",
                        principalTable: "venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservation_items",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: true),
                    unit_price_amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    UpdatedBy = table.Column<string>(type: "text", nullable: true)
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

            migrationBuilder.CreateTable(
                name: "allocations",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TicketTypeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalQuantity = table.Column<int>(type: "integer", nullable: false),
                    AllocatedQuantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UsedQuantity = table.Column<int>(type: "integer", nullable: false),
                    AccessCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    allowed_user_ids = table.Column<string>(type: "jsonb", nullable: true),
                    allowed_email_domains = table.Column<string>(type: "jsonb", nullable: true),
                    AvailableFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    allocated_seat_ids = table.Column<string>(type: "jsonb", nullable: false),
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
                name: "IX_AuditLogs_Entity_Timestamp",
                schema: "event",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                schema: "event",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                schema: "event",
                table: "AuditLogs",
                column: "UserId",
                filter: "\"UserId\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_Events_ActiveEvents",
                schema: "event",
                table: "events",
                columns: new[] { "EventDate", "Status" },
                filter: "\"Status\" IN ('Published', 'OnSale')");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Organization_Slug",
                schema: "event",
                table: "events",
                columns: new[] { "OrganizationId", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_Organization_Status_EventDate",
                schema: "event",
                table: "events",
                columns: new[] { "OrganizationId", "Status", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_PromoterId",
                schema: "event",
                table: "events",
                column: "PromoterId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_SearchVector",
                schema: "event",
                table: "events",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_Events_Status_EventDate",
                schema: "event",
                table: "events",
                columns: new[] { "Status", "EventDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Events_VenueId",
                schema: "event",
                table: "events",
                column: "VenueId");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rules_EventId1",
                schema: "event",
                table: "pricing_rules",
                column: "EventId1");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_DiscountCode_Active",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "DiscountCode", "IsActive" },
                filter: "\"DiscountCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Event_Active_Effective",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "EventId", "IsActive", "EffectiveFrom", "EffectiveTo" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Event_Priority_Active",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "EventId", "Priority", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Event_Type_Active",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "EventId", "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_EventId",
                schema: "event",
                table: "pricing_rules",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Type_Effective",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "Type", "EffectiveFrom", "EffectiveTo" },
                filter: "\"Type\" = 'TimeBased'");

            migrationBuilder.CreateIndex(
                name: "IX_PricingRules_Usage",
                schema: "event",
                table: "pricing_rules",
                columns: new[] { "MaxUses", "CurrentUses" },
                filter: "\"MaxUses\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_Seats_AllocatedToTicketTypeId",
                schema: "event",
                table: "seats",
                column: "AllocatedToTicketTypeId",
                filter: "\"AllocatedToTicketTypeId\" IS NOT NULL");

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

            migrationBuilder.CreateIndex(
                name: "IX_Seats_TicketType_Status",
                schema: "event",
                table: "seats",
                columns: new[] { "AllocatedToTicketTypeId", "Status" },
                filter: "\"AllocatedToTicketTypeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Venue_PriceCategory",
                schema: "event",
                table: "seats",
                columns: new[] { "VenueId", "PriceCategory" },
                filter: "\"PriceCategory\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Venue_Status",
                schema: "event",
                table: "seats",
                columns: new[] { "VenueId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Seats_Venue_Status_Accessible",
                schema: "event",
                table: "seats",
                columns: new[] { "VenueId", "Status", "IsAccessible" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_Available",
                schema: "event",
                table: "ticket_types",
                columns: new[] { "EventId", "IsVisible" },
                filter: "available_capacity > 0 AND \"IsVisible\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_Event_Code",
                schema: "event",
                table: "ticket_types",
                columns: new[] { "EventId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_Event_InventoryType",
                schema: "event",
                table: "ticket_types",
                columns: new[] { "EventId", "InventoryType" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketTypes_EventId",
                schema: "event",
                table: "ticket_types",
                column: "EventId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allocations",
                schema: "event");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "event");

            migrationBuilder.DropTable(
                name: "event_series",
                schema: "event");

            migrationBuilder.DropTable(
                name: "pricing_rules",
                schema: "event");

            migrationBuilder.DropTable(
                name: "reservation_items",
                schema: "event");

            migrationBuilder.DropTable(
                name: "seats",
                schema: "event");

            migrationBuilder.DropTable(
                name: "ticket_types",
                schema: "event");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "event");

            migrationBuilder.DropTable(
                name: "venues",
                schema: "event");

            migrationBuilder.DropTable(
                name: "events",
                schema: "event");
        }
    }
}
