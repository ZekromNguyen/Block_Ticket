using Event.Domain.Enums;
using Event.Domain.Exceptions;
using Event.Domain.ValueObjects;
using Shared.Common.Models;

namespace Event.Domain.Entities;

/// <summary>
/// Asset version entity - represents a specific version of a marketing asset
/// </summary>
public class AssetVersion : BaseAuditableEntity
{
    // Basic Properties
    public Guid AssetId { get; private set; }
    public int VersionNumber { get; private set; }
    public string ChangeDescription { get; private set; } = string.Empty;
    
    // File Information
    public AssetFileInfo FileInfo { get; private set; } = null!;
    public AssetStorageInfo StorageInfo { get; private set; } = null!;
    public AssetDimensions? Dimensions { get; private set; }
    
    // Processing Information
    public DateTime? ProcessedAt { get; private set; }
    public string? ProcessingLog { get; private set; }
    public bool IsProcessed { get; private set; }
    
    // Usage Tracking
    public int UsageCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    // Navigation Properties
    public MarketingAsset Asset { get; private set; } = null!;

    // Private constructor for EF Core
    private AssetVersion() { }

    public AssetVersion(
        Guid assetId,
        int versionNumber,
        AssetFileInfo fileInfo,
        AssetStorageInfo storageInfo,
        string changeDescription)
    {
        if (assetId == Guid.Empty)
            throw new EventDomainException("Asset ID cannot be empty");

        if (versionNumber <= 0)
            throw new EventDomainException("Version number must be greater than zero");

        if (string.IsNullOrWhiteSpace(changeDescription))
            throw new EventDomainException("Change description cannot be empty");

        Id = Guid.NewGuid();
        AssetId = assetId;
        VersionNumber = versionNumber;
        ChangeDescription = changeDescription.Trim();
        FileInfo = fileInfo ?? throw new EventDomainException("File info cannot be null");
        StorageInfo = storageInfo ?? throw new EventDomainException("Storage info cannot be null");
        IsProcessed = false;
        UsageCount = 0;
    }

    public void SetDimensions(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new EventDomainException("Dimensions must be greater than zero");

        Dimensions = new AssetDimensions(width, height);
    }

    public void MarkAsProcessed(string? processingLog = null)
    {
        IsProcessed = true;
        ProcessedAt = DateTime.UtcNow;
        ProcessingLog = processingLog?.Trim();
    }

    public void IncrementUsage()
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
    }

    public void UpdateStorageInfo(AssetStorageInfo storageInfo)
    {
        StorageInfo = storageInfo ?? throw new EventDomainException("Storage info cannot be null");
    }

    public string GetUrl(AssetQuality quality = AssetQuality.Original)
    {
        return StorageInfo.GetUrlForQuality(quality);
    }

    public bool HasThumbnail() => !string.IsNullOrEmpty(StorageInfo.ThumbnailUrl);
    
    public string? GetThumbnailUrl() => StorageInfo.ThumbnailUrl;
}
