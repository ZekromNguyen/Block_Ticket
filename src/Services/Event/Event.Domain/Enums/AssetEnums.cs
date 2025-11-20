namespace Event.Domain.Enums;

/// <summary>
/// Types of marketing assets
/// </summary>
public enum AssetType
{
    /// <summary>
    /// Image assets (JPEG, PNG, WebP, etc.)
    /// </summary>
    Image = 0,
    
    /// <summary>
    /// Video assets (MP4, WebM, etc.)
    /// </summary>
    Video = 1,
    
    /// <summary>
    /// Audio assets (MP3, WAV, etc.)
    /// </summary>
    Audio = 2,
    
    /// <summary>
    /// Document assets (PDF, DOC, etc.)
    /// </summary>
    Document = 3,
    
    /// <summary>
    /// Banner/promotional graphics
    /// </summary>
    Banner = 4,
    
    /// <summary>
    /// Logo assets
    /// </summary>
    Logo = 5,
    
    /// <summary>
    /// Social media assets
    /// </summary>
    SocialMedia = 6,
    
    /// <summary>
    /// Email template assets
    /// </summary>
    EmailTemplate = 7,
    
    /// <summary>
    /// Web graphics and UI elements
    /// </summary>
    WebGraphic = 8,
    
    /// <summary>
    /// Print materials
    /// </summary>
    Print = 9
}

/// <summary>
/// Asset processing status
/// </summary>
public enum AssetStatus
{
    /// <summary>
    /// Asset is being uploaded
    /// </summary>
    Uploading = 0,
    
    /// <summary>
    /// Asset is being processed (resizing, optimization, etc.)
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Asset is ready for use
    /// </summary>
    Ready = 2,
    
    /// <summary>
    /// Asset processing failed
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Asset is under review/approval
    /// </summary>
    UnderReview = 4,
    
    /// <summary>
    /// Asset has been approved
    /// </summary>
    Approved = 5,
    
    /// <summary>
    /// Asset has been rejected
    /// </summary>
    Rejected = 6,
    
    /// <summary>
    /// Asset is archived/inactive
    /// </summary>
    Archived = 7
}

/// <summary>
/// Asset usage context
/// </summary>
public enum AssetUsageContext
{
    /// <summary>
    /// General marketing use
    /// </summary>
    General = 0,
    
    /// <summary>
    /// Event-specific marketing
    /// </summary>
    Event = 1,
    
    /// <summary>
    /// Venue-specific marketing
    /// </summary>
    Venue = 2,
    
    /// <summary>
    /// Event series marketing
    /// </summary>
    EventSeries = 3,
    
    /// <summary>
    /// Social media campaigns
    /// </summary>
    SocialMedia = 4,
    
    /// <summary>
    /// Email marketing
    /// </summary>
    Email = 5,
    
    /// <summary>
    /// Website/web marketing
    /// </summary>
    Web = 6,
    
    /// <summary>
    /// Print advertising
    /// </summary>
    Print = 7,
    
    /// <summary>
    /// Mobile app marketing
    /// </summary>
    Mobile = 8
}

/// <summary>
/// Asset quality/resolution levels
/// </summary>
public enum AssetQuality
{
    /// <summary>
    /// Low quality/thumbnail (e.g., 150x150)
    /// </summary>
    Thumbnail = 0,
    
    /// <summary>
    /// Low quality for web preview (e.g., 400x300)
    /// </summary>
    Low = 1,
    
    /// <summary>
    /// Medium quality for web display (e.g., 800x600)
    /// </summary>
    Medium = 2,
    
    /// <summary>
    /// High quality for web/social media (e.g., 1200x900)
    /// </summary>
    High = 3,
    
    /// <summary>
    /// Ultra high quality for print (e.g., 2400x1800)
    /// </summary>
    UltraHigh = 4,
    
    /// <summary>
    /// Original uploaded quality
    /// </summary>
    Original = 5
}

/// <summary>
/// Marketing campaign status
/// </summary>
public enum CampaignStatus
{
    /// <summary>
    /// Campaign is being created/drafted
    /// </summary>
    Draft = 0,
    
    /// <summary>
    /// Campaign is scheduled to start
    /// </summary>
    Scheduled = 1,
    
    /// <summary>
    /// Campaign is currently active
    /// </summary>
    Active = 2,
    
    /// <summary>
    /// Campaign is paused
    /// </summary>
    Paused = 3,
    
    /// <summary>
    /// Campaign has completed
    /// </summary>
    Completed = 4,
    
    /// <summary>
    /// Campaign was cancelled
    /// </summary>
    Cancelled = 5,
    
    /// <summary>
    /// Campaign is archived
    /// </summary>
    Archived = 6
}

/// <summary>
/// A/B test variant status
/// </summary>
public enum VariantStatus
{
    /// <summary>
    /// Variant is active and receiving traffic
    /// </summary>
    Active = 0,
    
    /// <summary>
    /// Variant is paused
    /// </summary>
    Paused = 1,
    
    /// <summary>
    /// Variant is the winning variant
    /// </summary>
    Winner = 2,
    
    /// <summary>
    /// Variant has been disabled
    /// </summary>
    Disabled = 3
}

/// <summary>
/// Brand compliance validation status
/// </summary>
public enum ComplianceStatus
{
    /// <summary>
    /// Not yet validated
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Passed all compliance checks
    /// </summary>
    Compliant = 1,
    
    /// <summary>
    /// Failed compliance checks
    /// </summary>
    NonCompliant = 2,
    
    /// <summary>
    /// Requires manual review
    /// </summary>
    RequiresReview = 3,
    
    /// <summary>
    /// Compliance check failed due to error
    /// </summary>
    Error = 4
}
