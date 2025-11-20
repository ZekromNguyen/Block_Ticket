using Event.Domain.Enums;
using Event.Domain.Events;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Marketing asset aggregate root - represents a marketing asset with versioning and metadata
/// </summary>
public class MarketingAsset : BaseAuditableEntity
{
    private readonly List<AssetVersion> _versions = new();
    private readonly List<string> _tags = new();
    private readonly List<AssetUsageContext> _usageContexts = new();

    // Basic Properties
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AssetType Type { get; private set; }
    public AssetStatus Status { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? CategoryId { get; private set; }

    // File Information
    public AssetFileInfo FileInfo { get; private set; } = null!;
    public AssetDimensions? Dimensions { get; private set; }
    public AssetStorageInfo StorageInfo { get; private set; } = null!;
    public AssetMetadata Metadata { get; private set; } = new();

    // Versioning
    public int CurrentVersion { get; private set; } = 1;
    public Guid? ParentAssetId { get; private set; } // For asset variations/derivatives

    // Usage Tracking
    public int UsageCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    // Approval and Compliance
    public ComplianceValidationResult? ComplianceResult { get; private set; }
    public Guid? ApprovalWorkflowId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ApprovedBy { get; private set; }

    // Expiration
    public DateTime? ExpiresAt { get; private set; }

    // Navigation Properties
    public IReadOnlyCollection<AssetVersion> Versions => _versions.AsReadOnly();
    public IReadOnlyCollection<string> Tags => _tags.AsReadOnly();
    public IReadOnlyCollection<AssetUsageContext> UsageContexts => _usageContexts.AsReadOnly();

    // Private constructor for EF Core
    private MarketingAsset() { }

    public MarketingAsset(
        string name,
        string description,
        AssetType type,
        Guid organizationId,
        AssetFileInfo fileInfo,
        AssetStorageInfo storageInfo,
        Guid? categoryId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Asset name cannot be empty");

        if (organizationId == Guid.Empty)
            throw new EventDomainException("Organization ID cannot be empty");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        Type = type;
        Status = AssetStatus.Uploading;
        OrganizationId = organizationId;
        CategoryId = categoryId;
        FileInfo = fileInfo ?? throw new EventDomainException("File info cannot be null");
        StorageInfo = storageInfo ?? throw new EventDomainException("Storage info cannot be null");
        CurrentVersion = 1;
        UsageCount = 0;

        // Create initial version
        var initialVersion = new AssetVersion(Id, 1, fileInfo, storageInfo, "Initial upload");
        _versions.Add(initialVersion);

        // Raise domain event
        AddDomainEvent(new AssetCreatedDomainEvent(Id, Name, Type, OrganizationId));
    }

    public void UpdateBasicInfo(string name, string description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new EventDomainException("Asset name cannot be empty");

        var oldName = Name;
        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;

        if (oldName != Name)
        {
            AddDomainEvent(new AssetUpdatedDomainEvent(Id, Name, Type));
        }
    }

    public void SetDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new EventDomainException("Dimensions must be greater than zero");

        Dimensions = new AssetDimensions(width, height);
    }

    public void SetStatus(AssetStatus status)
    {
        if (Status == status) return;

        var oldStatus = Status;
        Status = status;

        AddDomainEvent(new AssetStatusChangedDomainEvent(Id, oldStatus, status));
    }

    public void SetMetadata(AssetMetadata metadata)
    {
        Metadata = metadata ?? new AssetMetadata();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new EventDomainException("Tag cannot be empty");

        var normalizedTag = tag.Trim().ToLowerInvariant();
        if (!_tags.Contains(normalizedTag))
        {
            _tags.Add(normalizedTag);
        }
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;

        var normalizedTag = tag.Trim().ToLowerInvariant();
        _tags.Remove(normalizedTag);
    }

    public void AddUsageContext(AssetUsageContext context)
    {
        if (!_usageContexts.Contains(context))
        {
            _usageContexts.Add(context);
        }
    }

    public void RemoveUsageContext(AssetUsageContext context)
    {
        _usageContexts.Remove(context);
    }

    public void IncrementUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    public AssetVersion CreateNewVersion(AssetFileInfo fileInfo, AssetStorageInfo storageInfo, string changeDescription)
    {
        if (fileInfo == null)
            throw new EventDomainException("File info cannot be null");

        if (storageInfo == null)
            throw new EventDomainException("Storage info cannot be null");

        CurrentVersion++;
        var newVersion = new AssetVersion(Id, CurrentVersion, fileInfo, storageInfo, changeDescription);
        _versions.Add(newVersion);

        // Update main asset info
        FileInfo = fileInfo;
        StorageInfo = storageInfo;
        Status = AssetStatus.Processing;

        AddDomainEvent(new AssetVersionCreatedDomainEvent(Id, CurrentVersion, changeDescription));

        return newVersion;
    }

    public AssetVersion? GetVersion(int versionNumber)
    {
        return _versions.FirstOrDefault(v => v.VersionNumber == versionNumber);
    }

    public AssetVersion GetCurrentVersion()
    {
        return _versions.First(v => v.VersionNumber == CurrentVersion);
    }

    public void SetComplianceResult(ComplianceValidationResult result)
    {
        ComplianceResult = result ?? throw new EventDomainException("Compliance result cannot be null");

        if (result.Status == ComplianceStatus.Compliant)
        {
            SetStatus(AssetStatus.Approved);
        }
        else if (result.Status == ComplianceStatus.NonCompliant)
        {
            SetStatus(AssetStatus.Rejected);
        }

        AddDomainEvent(new AssetComplianceValidatedDomainEvent(Id, result.Status, result.ComplianceScore));
    }

    public void Approve(string approvedBy, Guid? workflowId = null)
    {
        if (string.IsNullOrWhiteSpace(approvedBy))
            throw new EventDomainException("Approved by cannot be empty");

        ApprovedAt = DateTime.UtcNow;
        ApprovedBy = approvedBy.Trim();
        ApprovalWorkflowId = workflowId;
        SetStatus(AssetStatus.Approved);

        AddDomainEvent(new AssetApprovedDomainEvent(Id, approvedBy, workflowId));
    }

    public void Reject(string reason)
    {
        SetStatus(AssetStatus.Rejected);
        AddDomainEvent(new AssetRejectedDomainEvent(Id, reason));
    }

    public void SetExpiration(DateTime expiresAt)
    {
        if (expiresAt <= DateTime.UtcNow)
            throw new EventDomainException("Expiration date must be in the future");

        ExpiresAt = expiresAt;
    }

    public void Archive()
    {
        SetStatus(AssetStatus.Archived);
        AddDomainEvent(new AssetArchivedDomainEvent(Id));
    }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
    public bool IsReady() => Status == AssetStatus.Ready || Status == AssetStatus.Approved;
    public bool CanBeUsed() => IsReady() && !IsExpired();
    public bool RequiresApproval() => Status == AssetStatus.UnderReview;
}
