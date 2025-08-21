using Event.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace Event.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for ApprovalWorkflow
/// </summary>
public class ApprovalWorkflowConfiguration : IEntityTypeConfiguration<ApprovalWorkflow>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        // Table configuration
        builder.ToTable("approval_workflows");
        builder.HasKey(w => w.Id);

        // Basic properties
        builder.Property(w => w.OrganizationId)
            .IsRequired();

        builder.Property(w => w.RequesterId)
            .IsRequired();

        builder.Property(w => w.RequesterName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.RequesterEmail)
            .IsRequired()
            .HasMaxLength(200);

        // Operation details
        builder.Property(w => w.OperationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(w => w.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(w => w.EntityId)
            .IsRequired();

        builder.Property(w => w.OperationDescription)
            .IsRequired()
            .HasMaxLength(1000);

        // Status and approval details
        builder.Property(w => w.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ApprovalStatus.Pending);

        builder.Property(w => w.RequiredApprovals)
            .IsRequired()
            .HasDefaultValue(2);

        builder.Property(w => w.CurrentApprovals)
            .IsRequired()
            .HasDefaultValue(0);

        // Text fields
        builder.Property(w => w.BusinessJustification)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(w => w.ExpectedImpact)
            .HasMaxLength(1000);

        builder.Property(w => w.CompletionReason)
            .HasMaxLength(1000);

        // Enums
        builder.Property(w => w.RiskLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RiskLevel.Medium);

        builder.Property(w => w.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Priority.Normal);

        // JSON fields
        builder.Property(w => w.OperationData)
            .HasColumnType("jsonb");

        builder.Property(w => w.Tags)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        builder.Property(w => w.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        // Timestamps
        builder.Property(w => w.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(w => w.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(w => w.ExpiresAt)
            .IsRequired();

        builder.Property(w => w.CompletedAt);

        // Relationships
        builder.HasMany(w => w.ApprovalSteps)
            .WithOne()
            .HasForeignKey(s => s.ApprovalWorkflowId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        ConfigureIndexes(builder);
    }

    private static void ConfigureIndexes(EntityTypeBuilder<ApprovalWorkflow> builder)
    {
        // Primary indexes for querying
        builder.HasIndex(w => w.OrganizationId)
            .HasDatabaseName("IX_ApprovalWorkflows_OrganizationId");

        builder.HasIndex(w => w.Status)
            .HasDatabaseName("IX_ApprovalWorkflows_Status");

        builder.HasIndex(w => w.OperationType)
            .HasDatabaseName("IX_ApprovalWorkflows_OperationType");

        builder.HasIndex(w => w.RequesterId)
            .HasDatabaseName("IX_ApprovalWorkflows_RequesterId");

        builder.HasIndex(w => new { w.EntityType, w.EntityId })
            .HasDatabaseName("IX_ApprovalWorkflows_Entity");

        // Composite indexes for common queries
        builder.HasIndex(w => new { w.OrganizationId, w.Status })
            .HasDatabaseName("IX_ApprovalWorkflows_Organization_Status");

        builder.HasIndex(w => new { w.Status, w.ExpiresAt })
            .HasDatabaseName("IX_ApprovalWorkflows_Status_ExpiresAt");

        builder.HasIndex(w => new { w.OrganizationId, w.CreatedAt })
            .HasDatabaseName("IX_ApprovalWorkflows_Organization_Created");

        // Performance indexes
        builder.HasIndex(w => w.ExpiresAt)
            .HasDatabaseName("IX_ApprovalWorkflows_ExpiresAt")
            .HasFilter("\"Status\" = 'Pending'");

        builder.HasIndex(w => w.CreatedAt)
            .HasDatabaseName("IX_ApprovalWorkflows_CreatedAt");

        builder.HasIndex(w => w.Priority)
            .HasDatabaseName("IX_ApprovalWorkflows_Priority");

        builder.HasIndex(w => w.RiskLevel)
            .HasDatabaseName("IX_ApprovalWorkflows_RiskLevel");
    }
}

/// <summary>
/// Entity Framework configuration for ApprovalStep
/// </summary>
public class ApprovalStepConfiguration : IEntityTypeConfiguration<ApprovalStep>
{
    public void Configure(EntityTypeBuilder<ApprovalStep> builder)
    {
        // Table configuration
        builder.ToTable("approval_steps");
        builder.HasKey(s => s.Id);

        // Foreign key
        builder.Property(s => s.ApprovalWorkflowId)
            .IsRequired();

        // Approver details
        builder.Property(s => s.ApproverId)
            .IsRequired();

        builder.Property(s => s.ApproverName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ApproverEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.ApproverRole)
            .HasMaxLength(100);

        // Decision details
        builder.Property(s => s.Decision)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(ApprovalDecision.Pending);

        builder.Property(s => s.Comments)
            .HasMaxLength(2000);

        // Timestamps
        builder.Property(s => s.DecisionAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Security/audit fields
        builder.Property(s => s.IpAddress)
            .HasMaxLength(45); // IPv6 support

        builder.Property(s => s.UserAgent)
            .HasMaxLength(500);

        // JSON metadata
        builder.Property(s => s.DecisionMetadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(s => s.ApprovalWorkflowId)
            .HasDatabaseName("IX_ApprovalSteps_WorkflowId");

        builder.HasIndex(s => s.ApproverId)
            .HasDatabaseName("IX_ApprovalSteps_ApproverId");

        builder.HasIndex(s => s.Decision)
            .HasDatabaseName("IX_ApprovalSteps_Decision");

        builder.HasIndex(s => s.DecisionAt)
            .HasDatabaseName("IX_ApprovalSteps_DecisionAt");

        builder.HasIndex(s => new { s.ApprovalWorkflowId, s.ApproverId })
            .HasDatabaseName("IX_ApprovalSteps_Workflow_Approver")
            .IsUnique();
    }
}

/// <summary>
/// Entity Framework configuration for ApprovalWorkflowTemplate
/// </summary>
public class ApprovalWorkflowTemplateConfiguration : IEntityTypeConfiguration<ApprovalWorkflowTemplate>
{
    public void Configure(EntityTypeBuilder<ApprovalWorkflowTemplate> builder)
    {
        // Table configuration
        builder.ToTable("approval_workflow_templates");
        builder.HasKey(t => t.Id);

        // Basic properties
        builder.Property(t => t.OrganizationId)
            .IsRequired();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(1000);

        builder.Property(t => t.OperationType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.RequiredApprovals)
            .IsRequired()
            .HasDefaultValue(2);

        // JSON fields
        builder.Property(t => t.RequiredRoles)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
            .HasColumnType("jsonb");

        // Enums
        builder.Property(t => t.DefaultRiskLevel)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(RiskLevel.Medium);

        // Time span
        builder.Property(t => t.DefaultExpirationTime)
            .IsRequired()
            .HasConversion(
                v => v.Ticks,
                v => new TimeSpan(v))
            .HasDefaultValue(TimeSpan.FromDays(7));

        // Status
        builder.Property(t => t.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Timestamps
        builder.Property(t => t.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.Property(t => t.UpdatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Complex objects (would need custom converters)
        builder.Ignore(t => t.AutoApprovalConditions);
        builder.Ignore(t => t.EscalationRules);

        // Indexes
        builder.HasIndex(t => t.OrganizationId)
            .HasDatabaseName("IX_ApprovalTemplates_OrganizationId");

        builder.HasIndex(t => new { t.OrganizationId, t.OperationType })
            .HasDatabaseName("IX_ApprovalTemplates_Organization_Operation")
            .IsUnique();

        builder.HasIndex(t => t.OperationType)
            .HasDatabaseName("IX_ApprovalTemplates_OperationType");

        builder.HasIndex(t => t.IsActive)
            .HasDatabaseName("IX_ApprovalTemplates_IsActive");
    }
}

/// <summary>
/// Entity Framework configuration for ApprovalAuditLog
/// </summary>
public class ApprovalAuditLogConfiguration : IEntityTypeConfiguration<ApprovalAuditLog>
{
    public void Configure(EntityTypeBuilder<ApprovalAuditLog> builder)
    {
        // Table configuration
        builder.ToTable("approval_audit_logs");
        builder.HasKey(a => a.Id);

        // Foreign key
        builder.Property(a => a.ApprovalWorkflowId)
            .IsRequired();

        // User details
        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.UserName)
            .IsRequired()
            .HasMaxLength(200);

        // Action details
        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.Details)
            .IsRequired()
            .HasMaxLength(2000);

        // Timestamp
        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Security fields
        builder.Property(a => a.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500);

        // JSON metadata
        builder.Property(a => a.Metadata)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>())
            .HasColumnType("jsonb");

        // Indexes
        builder.HasIndex(a => a.ApprovalWorkflowId)
            .HasDatabaseName("IX_ApprovalAuditLogs_WorkflowId");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_ApprovalAuditLogs_UserId");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_ApprovalAuditLogs_Action");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_ApprovalAuditLogs_Timestamp");

        builder.HasIndex(a => new { a.ApprovalWorkflowId, a.Timestamp })
            .HasDatabaseName("IX_ApprovalAuditLogs_Workflow_Timestamp");
    }
}
