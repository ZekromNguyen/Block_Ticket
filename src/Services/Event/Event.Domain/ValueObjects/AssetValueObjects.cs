using Event.Domain.Enums;
using Event.Domain.Exceptions;

namespace Event.Domain.ValueObjects;

/// <summary>
/// Asset file information value object
/// </summary>
public record AssetFileInfo
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string FileExtension { get; init; } = string.Empty;
    public string? Checksum { get; init; }

    public AssetFileInfo(string fileName, string contentType, long fileSizeBytes, string? checksum = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            throw new EventDomainException("File name cannot be empty");
        
        if (string.IsNullOrWhiteSpace(contentType))
            throw new EventDomainException("Content type cannot be empty");
        
        if (fileSizeBytes <= 0)
            throw new EventDomainException("File size must be greater than zero");

        FileName = fileName.Trim();
        ContentType = contentType.Trim().ToLowerInvariant();
        FileSizeBytes = fileSizeBytes;
        FileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        Checksum = checksum?.Trim();
    }

    public string GetFormattedFileSize()
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = FileSizeBytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public bool IsImage() => ContentType.StartsWith("image/");
    public bool IsVideo() => ContentType.StartsWith("video/");
    public bool IsAudio() => ContentType.StartsWith("audio/");
    public bool IsDocument() => ContentType.StartsWith("application/") || ContentType.StartsWith("text/");
}

/// <summary>
/// Asset dimensions for images and videos
/// </summary>
public record AssetDimensions
{
    public int Width { get; init; }
    public int Height { get; init; }
    public double AspectRatio => Height > 0 ? (double)Width / Height : 0;

    public AssetDimensions(int width, int height)
    {
        if (width <= 0)
            throw new EventDomainException("Width must be greater than zero");
        
        if (height <= 0)
            throw new EventDomainException("Height must be greater than zero");

        Width = width;
        Height = height;
    }

    public bool IsSquare() => Width == Height;
    public bool IsLandscape() => Width > Height;
    public bool IsPortrait() => Height > Width;
    
    public string GetAspectRatioString()
    {
        var gcd = CalculateGCD(Width, Height);
        return $"{Width / gcd}:{Height / gcd}";
    }

    private static int CalculateGCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
}

/// <summary>
/// Asset storage location information
/// </summary>
public record AssetStorageInfo
{
    public string StorageProvider { get; init; } = string.Empty;
    public string StoragePath { get; init; } = string.Empty;
    public string? CdnUrl { get; init; }
    public string? ThumbnailUrl { get; init; }
    public Dictionary<AssetQuality, string> QualityUrls { get; init; } = new();

    public AssetStorageInfo(string storageProvider, string storagePath, string? cdnUrl = null)
    {
        if (string.IsNullOrWhiteSpace(storageProvider))
            throw new EventDomainException("Storage provider cannot be empty");
        
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new EventDomainException("Storage path cannot be empty");

        StorageProvider = storageProvider.Trim();
        StoragePath = storagePath.Trim();
        CdnUrl = cdnUrl?.Trim();
    }

    public string GetUrlForQuality(AssetQuality quality)
    {
        return QualityUrls.TryGetValue(quality, out var url) ? url : CdnUrl ?? StoragePath;
    }

    public AssetStorageInfo WithThumbnail(string thumbnailUrl)
    {
        return this with { ThumbnailUrl = thumbnailUrl };
    }

    public AssetStorageInfo WithQualityUrl(AssetQuality quality, string url)
    {
        var newQualityUrls = new Dictionary<AssetQuality, string>(QualityUrls)
        {
            [quality] = url
        };
        return this with { QualityUrls = newQualityUrls };
    }
}

/// <summary>
/// Asset metadata for additional properties
/// </summary>
public record AssetMetadata
{
    public Dictionary<string, object> Properties { get; init; } = new();
    public List<string> Keywords { get; init; } = new();
    public string? AltText { get; init; }
    public string? Caption { get; init; }
    public string? Copyright { get; init; }
    public string? Attribution { get; init; }

    public AssetMetadata() { }

    public AssetMetadata(Dictionary<string, object>? properties = null, List<string>? keywords = null)
    {
        Properties = properties ?? new Dictionary<string, object>();
        Keywords = keywords ?? new List<string>();
    }

    public T? GetProperty<T>(string key)
    {
        if (Properties.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return default;
    }

    public AssetMetadata WithProperty(string key, object value)
    {
        var newProperties = new Dictionary<string, object>(Properties)
        {
            [key] = value
        };
        return this with { Properties = newProperties };
    }

    public AssetMetadata WithKeyword(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword) || Keywords.Contains(keyword, StringComparer.OrdinalIgnoreCase))
            return this;

        var newKeywords = new List<string>(Keywords) { keyword.Trim() };
        return this with { Keywords = newKeywords };
    }

    public AssetMetadata WithAltText(string altText)
    {
        return this with { AltText = altText?.Trim() };
    }

    public AssetMetadata WithCaption(string caption)
    {
        return this with { Caption = caption?.Trim() };
    }
}

/// <summary>
/// Brand compliance validation result
/// </summary>
public record ComplianceValidationResult
{
    public ComplianceStatus Status { get; init; }
    public List<string> Violations { get; init; } = new();
    public List<string> Warnings { get; init; } = new();
    public double ComplianceScore { get; init; }
    public DateTime ValidatedAt { get; init; }
    public string? ValidatedBy { get; init; }

    public ComplianceValidationResult(ComplianceStatus status, double complianceScore)
    {
        Status = status;
        ComplianceScore = Math.Max(0, Math.Min(100, complianceScore));
        ValidatedAt = DateTime.UtcNow;
    }

    public bool IsCompliant => Status == ComplianceStatus.Compliant;
    public bool HasViolations => Violations.Any();
    public bool HasWarnings => Warnings.Any();

    public ComplianceValidationResult WithViolation(string violation)
    {
        var newViolations = new List<string>(Violations) { violation };
        return this with { Violations = newViolations };
    }

    public ComplianceValidationResult WithWarning(string warning)
    {
        var newWarnings = new List<string>(Warnings) { warning };
        return this with { Warnings = newWarnings };
    }
}
