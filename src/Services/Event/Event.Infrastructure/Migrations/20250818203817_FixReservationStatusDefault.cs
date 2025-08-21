using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Event.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixReservationStatusDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SeatMap",
                schema: "event",
                table: "venues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeatMapVersion",
                schema: "event",
                table: "venues",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ETagUpdatedAt",
                schema: "event",
                table: "ticket_types",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ETagValue",
                schema: "event",
                table: "ticket_types",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CustomerEmail",
                schema: "event",
                table: "reservations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                schema: "event",
                table: "events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAt",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDateTime",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                schema: "event",
                table: "events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "PublishedAt",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDateTime",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "etag_updated_at",
                schema: "event",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "etag_value",
                schema: "event",
                table: "events",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<byte[]>(
                name: "row_version",
                schema: "event",
                table: "events",
                type: "bytea",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "approval_audit_logs",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_workflow_templates",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    RequiredRoles = table.Column<string>(type: "jsonb", nullable: false),
                    DefaultRiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    DefaultExpirationTime = table.Column<long>(type: "bigint", nullable: false, defaultValue: 6048000000000L),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_workflow_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_workflows",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequesterName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequesterEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    OperationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationDescription = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    CurrentApprovals = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    OperationData = table.Column<string>(type: "jsonb", nullable: false),
                    BusinessJustification = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Medium"),
                    ExpectedImpact = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Priority = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Normal"),
                    Tags = table.Column<string>(type: "jsonb", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_workflows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "approval_steps",
                schema: "event",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovalWorkflowId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproverName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverEmail = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ApproverRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Decision = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    Comments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    DecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DecisionMetadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_approval_steps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_approval_steps_approval_workflows_ApprovalWorkflowId",
                        column: x => x.ApprovalWorkflowId,
                        principalSchema: "event",
                        principalTable: "approval_workflows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventAggregate_ETag",
                schema: "event",
                table: "events",
                column: "etag_value");

            migrationBuilder.CreateIndex(
                name: "IX_EventAggregate_ETagTimestamp",
                schema: "event",
                table: "events",
                column: "etag_updated_at");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAggregate_ETag_NotEmpty",
                schema: "event",
                table: "events",
                sql: "LENGTH(etag_value) > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_EventAggregate_ETag_ValidTimestamp",
                schema: "event",
                table: "events",
                sql: "etag_updated_at <= NOW() AND etag_updated_at > '2024-01-01'::timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuditLogs_Action",
                schema: "event",
                table: "approval_audit_logs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuditLogs_Timestamp",
                schema: "event",
                table: "approval_audit_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuditLogs_UserId",
                schema: "event",
                table: "approval_audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuditLogs_Workflow_Timestamp",
                schema: "event",
                table: "approval_audit_logs",
                columns: new[] { "ApprovalWorkflowId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalAuditLogs_WorkflowId",
                schema: "event",
                table: "approval_audit_logs",
                column: "ApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_ApproverId",
                schema: "event",
                table: "approval_steps",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_Decision",
                schema: "event",
                table: "approval_steps",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_DecisionAt",
                schema: "event",
                table: "approval_steps",
                column: "DecisionAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_Workflow_Approver",
                schema: "event",
                table: "approval_steps",
                columns: new[] { "ApprovalWorkflowId", "ApproverId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalSteps_WorkflowId",
                schema: "event",
                table: "approval_steps",
                column: "ApprovalWorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTemplates_IsActive",
                schema: "event",
                table: "approval_workflow_templates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTemplates_OperationType",
                schema: "event",
                table: "approval_workflow_templates",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTemplates_Organization_Operation",
                schema: "event",
                table: "approval_workflow_templates",
                columns: new[] { "OrganizationId", "OperationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalTemplates_OrganizationId",
                schema: "event",
                table: "approval_workflow_templates",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_CreatedAt",
                schema: "event",
                table: "approval_workflows",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Entity",
                schema: "event",
                table: "approval_workflows",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_ExpiresAt",
                schema: "event",
                table: "approval_workflows",
                column: "ExpiresAt",
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_OperationType",
                schema: "event",
                table: "approval_workflows",
                column: "OperationType");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Organization_Created",
                schema: "event",
                table: "approval_workflows",
                columns: new[] { "OrganizationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Organization_Status",
                schema: "event",
                table: "approval_workflows",
                columns: new[] { "OrganizationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_OrganizationId",
                schema: "event",
                table: "approval_workflows",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Priority",
                schema: "event",
                table: "approval_workflows",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_RequesterId",
                schema: "event",
                table: "approval_workflows",
                column: "RequesterId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_RiskLevel",
                schema: "event",
                table: "approval_workflows",
                column: "RiskLevel");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Status",
                schema: "event",
                table: "approval_workflows",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalWorkflows_Status_ExpiresAt",
                schema: "event",
                table: "approval_workflows",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "approval_audit_logs",
                schema: "event");

            migrationBuilder.DropTable(
                name: "approval_steps",
                schema: "event");

            migrationBuilder.DropTable(
                name: "approval_workflow_templates",
                schema: "event");

            migrationBuilder.DropTable(
                name: "approval_workflows",
                schema: "event");

            migrationBuilder.DropIndex(
                name: "IX_EventAggregate_ETag",
                schema: "event",
                table: "events");

            migrationBuilder.DropIndex(
                name: "IX_EventAggregate_ETagTimestamp",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAggregate_ETag_NotEmpty",
                schema: "event",
                table: "events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_EventAggregate_ETag_ValidTimestamp",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "SeatMap",
                schema: "event",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "SeatMapVersion",
                schema: "event",
                table: "venues");

            migrationBuilder.DropColumn(
                name: "ETagUpdatedAt",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "ETagValue",
                schema: "event",
                table: "ticket_types");

            migrationBuilder.DropColumn(
                name: "CustomerEmail",
                schema: "event",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "EndDateTime",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "Name",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "PublishedAt",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "StartDateTime",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "etag_updated_at",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "etag_value",
                schema: "event",
                table: "events");

            migrationBuilder.DropColumn(
                name: "row_version",
                schema: "event",
                table: "events");
        }
    }
}
