using System;
using System.Collections.Generic;

namespace Event.Application.DTOs
{
    public class MarketingAssetDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid OrganizationId { get; set; }
        public Guid? CategoryId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string CdnUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public int CurrentVersion { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public MarketingAssetDto() { }
    }

    public record AssetVersionDto(
        Guid Id,
        Guid AssetId,
        int VersionNumber,
        string ChangeDescription,
        string FileName,
        string ContentType,
        long FileSizeBytes,
        string CdnUrl,
        string ThumbnailUrl,
        DateTime CreatedAt);

    public record AssetCategoryDto(
        Guid Id,
        string Name,
        string Description,
        string Slug,
        Guid? ParentCategoryId,
        int Level,
        string Path,
        bool IsActive,
        DateTime CreatedAt);

    public record MarketingCampaignDto(
        Guid Id,
        string Name,
        string Description,
        string Status,
        Guid OrganizationId,
        DateTime StartDate,
        DateTime? EndDate,
        bool IsABTest,
        DateTime CreatedAt);

    public record CampaignVariantDto(
        Guid Id,
        Guid CampaignId,
        string Name,
        string Description,
        string Status,
        double TrafficPercentage,
        bool IsControl,
        bool IsWinner,
        Guid PrimaryAssetId,
        DateTime CreatedAt);
}
