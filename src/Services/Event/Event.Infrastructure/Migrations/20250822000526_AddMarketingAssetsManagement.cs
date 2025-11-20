using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingAssetsManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "asset_categories",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IconUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_asset_categories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_categories_asset_categories_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalSchema: "event",
                        principalTable: "asset_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "marketing_campaigns",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    time_zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PrimaryContext = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsABTest = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ConfidenceThreshold = table.Column<double>(type: "double precision", nullable: true, defaultValue: 95.0),
                    TotalImpressions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalClicks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalConversions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    WinningVariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TestCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StatisticalSignificance = table.Column<double>(type: "double precision", nullable: true),
                    Budget = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TotalSpent = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    metrics = table.Column<string>(type: "jsonb", nullable: false),
                    target_audiences = table.Column<string>(type: "jsonb", nullable: false),
                    target_event_ids = table.Column<string>(type: "jsonb", nullable: false),
                    target_venue_ids = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_marketing_campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "marketing_assets",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    file_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    storage_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cdn_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quality_urls = table.Column<string>(type: "jsonb", nullable: false),
                    metadata_properties = table.Column<string>(type: "jsonb", nullable: false),
                    keywords = table.Column<string>(type: "jsonb", nullable: false),
                    alt_text = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    caption = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    copyright = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    attribution = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CurrentVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    ParentAssetId = table.Column<Guid>(type: "uuid", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    compliance_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    compliance_violations = table.Column<string>(type: "jsonb", nullable: true),
                    compliance_warnings = table.Column<string>(type: "jsonb", nullable: true),
                    compliance_score = table.Column<double>(type: "double precision", nullable: true),
                    compliance_validated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    compliance_validated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AssetCategoryId = table.Column<Guid>(type: "uuid", nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    usage_contexts = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_marketing_assets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_marketing_assets_asset_categories_AssetCategoryId",
                        column: x => x.AssetCategoryId,
                        principalSchema: "event",
                        principalTable: "asset_categories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_marketing_assets_asset_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "event",
                        principalTable: "asset_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "asset_versions",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    ChangeDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_extension = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    file_checksum = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    storage_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    cdn_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    quality_urls = table.Column<string>(type: "jsonb", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingLog = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_asset_versions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asset_versions_marketing_assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "event",
                        principalTable: "marketing_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "campaign_variants",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TrafficPercentage = table.Column<double>(type: "numeric(5,2)", nullable: false),
                    IsControl = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsWinner = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    PrimaryAssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalImpressions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalClicks = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalConversions = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    TotalConversionValue = table.Column<decimal>(type: "numeric(18,2)", nullable: false, defaultValue: 0m),
                    ConfidenceLevel = table.Column<double>(type: "numeric(5,2)", nullable: true),
                    StatisticalSignificance = table.Column<double>(type: "numeric(5,2)", nullable: true),
                    asset_ids = table.Column<string>(type: "jsonb", nullable: false),
                    metrics = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("PK_campaign_variants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_campaign_variants_marketing_assets_PrimaryAssetId",
                        column: x => x.PrimaryAssetId,
                        principalSchema: "event",
                        principalTable: "marketing_assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_campaign_variants_marketing_campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "event",
                        principalTable: "marketing_campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_active",
                schema: "event",
                table: "asset_categories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_level",
                schema: "event",
                table: "asset_categories",
                column: "Level");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_org_parent",
                schema: "event",
                table: "asset_categories",
                columns: new[] { "OrganizationId", "ParentCategoryId" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_org_parent_sort",
                schema: "event",
                table: "asset_categories",
                columns: new[] { "OrganizationId", "ParentCategoryId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_parent_id",
                schema: "event",
                table: "asset_categories",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_path",
                schema: "event",
                table: "asset_categories",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_rls",
                schema: "event",
                table: "asset_categories",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_search",
                schema: "event",
                table: "asset_categories",
                columns: new[] { "Name", "Description" });

            migrationBuilder.CreateIndex(
                name: "ix_asset_categories_unique_name_in_parent",
                schema: "event",
                table: "asset_categories",
                columns: new[] { "OrganizationId", "ParentCategoryId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_asset_id",
                schema: "event",
                table: "asset_versions",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_asset_version",
                schema: "event",
                table: "asset_versions",
                columns: new[] { "AssetId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_last_used",
                schema: "event",
                table: "asset_versions",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_processed",
                schema: "event",
                table: "asset_versions",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_processed_at",
                schema: "event",
                table: "asset_versions",
                column: "ProcessedAt");

            migrationBuilder.CreateIndex(
                name: "ix_asset_versions_usage_count",
                schema: "event",
                table: "asset_versions",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_campaign_id",
                schema: "event",
                table: "campaign_variants",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_campaign_status",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "CampaignId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_campaign_traffic",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "CampaignId", "TrafficPercentage" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_clicks",
                schema: "event",
                table: "campaign_variants",
                column: "TotalClicks");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_confidence",
                schema: "event",
                table: "campaign_variants",
                column: "ConfidenceLevel");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_control",
                schema: "event",
                table: "campaign_variants",
                column: "IsControl");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_conversion_value",
                schema: "event",
                table: "campaign_variants",
                column: "TotalConversionValue");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_conversions",
                schema: "event",
                table: "campaign_variants",
                column: "TotalConversions");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_impressions",
                schema: "event",
                table: "campaign_variants",
                column: "TotalImpressions");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_primary_asset",
                schema: "event",
                table: "campaign_variants",
                column: "PrimaryAssetId");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_search",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "Name", "Description" });

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_significance",
                schema: "event",
                table: "campaign_variants",
                column: "StatisticalSignificance");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_status",
                schema: "event",
                table: "campaign_variants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_traffic",
                schema: "event",
                table: "campaign_variants",
                column: "TrafficPercentage");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_unique_control",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "CampaignId", "IsControl" },
                unique: true,
                filter: "\"IsControl\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_unique_name",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "CampaignId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_unique_winner",
                schema: "event",
                table: "campaign_variants",
                columns: new[] { "CampaignId", "IsWinner" },
                unique: true,
                filter: "\"IsWinner\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_campaign_variants_winner",
                schema: "event",
                table: "campaign_variants",
                column: "IsWinner");

            migrationBuilder.CreateIndex(
                name: "IX_marketing_assets_AssetCategoryId",
                schema: "event",
                table: "marketing_assets",
                column: "AssetCategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_category_id",
                schema: "event",
                table: "marketing_assets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_last_used",
                schema: "event",
                table: "marketing_assets",
                column: "LastUsedAt");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_org_status",
                schema: "event",
                table: "marketing_assets",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_org_type",
                schema: "event",
                table: "marketing_assets",
                columns: new[] { "OrganizationId", "Type" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_rls",
                schema: "event",
                table: "marketing_assets",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_search",
                schema: "event",
                table: "marketing_assets",
                columns: new[] { "Name", "Description" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_status",
                schema: "event",
                table: "marketing_assets",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_type",
                schema: "event",
                table: "marketing_assets",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_assets_usage_count",
                schema: "event",
                table: "marketing_assets",
                column: "UsageCount");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_ab_test",
                schema: "event",
                table: "marketing_campaigns",
                column: "IsABTest");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_clicks",
                schema: "event",
                table: "marketing_campaigns",
                column: "TotalClicks");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_context",
                schema: "event",
                table: "marketing_campaigns",
                column: "PrimaryContext");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_conversions",
                schema: "event",
                table: "marketing_campaigns",
                column: "TotalConversions");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_date_range",
                schema: "event",
                table: "marketing_campaigns",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_end_date",
                schema: "event",
                table: "marketing_campaigns",
                column: "EndDate");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_impressions",
                schema: "event",
                table: "marketing_campaigns",
                column: "TotalImpressions");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_org_status",
                schema: "event",
                table: "marketing_campaigns",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_rls",
                schema: "event",
                table: "marketing_campaigns",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_search",
                schema: "event",
                table: "marketing_campaigns",
                columns: new[] { "Name", "Description" });

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_start_date",
                schema: "event",
                table: "marketing_campaigns",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_status",
                schema: "event",
                table: "marketing_campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_test_completed",
                schema: "event",
                table: "marketing_campaigns",
                column: "TestCompletedAt");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_total_spent",
                schema: "event",
                table: "marketing_campaigns",
                column: "TotalSpent");

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_unique_name",
                schema: "event",
                table: "marketing_campaigns",
                columns: new[] { "OrganizationId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_marketing_campaigns_winning_variant",
                schema: "event",
                table: "marketing_campaigns",
                column: "WinningVariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asset_versions",
                schema: "event");

            migrationBuilder.DropTable(
                name: "campaign_variants",
                schema: "event");

            migrationBuilder.DropTable(
                name: "marketing_assets",
                schema: "event");

            migrationBuilder.DropTable(
                name: "marketing_campaigns",
                schema: "event");

            migrationBuilder.DropTable(
                name: "asset_categories",
                schema: "event");
        }
    }
}
