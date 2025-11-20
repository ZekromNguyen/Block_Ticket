using Event.Domain.Enums;
using MediatR;
using Shared.Common.Models;

namespace Event.Domain.Events;

/// <summary>
/// Domain event raised when a marketing asset is created
/// </summary>
public record AssetCreatedDomainEvent(
    Guid AssetId,
    string Name,
    AssetType Type,
    Guid OrganizationId) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a marketing asset is updated
/// </summary>
public record AssetUpdatedDomainEvent(
    Guid AssetId,
    string Name,
    AssetType Type) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset status changes
/// </summary>
public record AssetStatusChangedDomainEvent(
    Guid AssetId,
    AssetStatus OldStatus,
    AssetStatus NewStatus) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a new asset version is created
/// </summary>
public record AssetVersionCreatedDomainEvent(
    Guid AssetId,
    int VersionNumber,
    string ChangeDescription) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset is approved
/// </summary>
public record AssetApprovedDomainEvent(
    Guid AssetId,
    string ApprovedBy,
    Guid? WorkflowId) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset is rejected
/// </summary>
public record AssetRejectedDomainEvent(
    Guid AssetId,
    string Reason) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset is archived
/// </summary>
public record AssetArchivedDomainEvent(
    Guid AssetId) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when asset compliance is validated
/// </summary>
public record AssetComplianceValidatedDomainEvent(
    Guid AssetId,
    ComplianceStatus Status,
    double ComplianceScore) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset is used/referenced
/// </summary>
public record AssetUsedDomainEvent(
    Guid AssetId,
    AssetUsageContext Context,
    Guid? RelatedEntityId,
    string? RelatedEntityType) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset category is created
/// </summary>
public record AssetCategoryCreatedDomainEvent(
    Guid CategoryId,
    string Name,
    Guid OrganizationId,
    Guid? ParentCategoryId) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset category is updated
/// </summary>
public record AssetCategoryUpdatedDomainEvent(
    Guid CategoryId,
    string Name) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when an asset category is deactivated
/// </summary>
public record AssetCategoryDeactivatedDomainEvent(
    Guid CategoryId,
    string Name) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a marketing campaign is created
/// </summary>
public record MarketingCampaignCreatedDomainEvent(
    Guid CampaignId,
    string Name,
    Guid OrganizationId,
    DateTime StartDate,
    DateTime? EndDate) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a marketing campaign status changes
/// </summary>
public record MarketingCampaignStatusChangedDomainEvent(
    Guid CampaignId,
    CampaignStatus OldStatus,
    CampaignStatus NewStatus) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when a campaign variant is created for A/B testing
/// </summary>
public record CampaignVariantCreatedDomainEvent(
    Guid CampaignId,
    Guid VariantId,
    string Name,
    double TrafficPercentage) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when A/B test results are available
/// </summary>
public record ABTestResultsAvailableDomainEvent(
    Guid CampaignId,
    Guid WinningVariantId,
    double ConfidenceLevel,
    Dictionary<string, double> Metrics) : IDomainEvent, INotification;

/// <summary>
/// Domain event raised when campaign performance metrics are updated
/// </summary>
public record CampaignMetricsUpdatedDomainEvent(
    Guid CampaignId,
    Dictionary<string, double> Metrics,
    DateTime MetricsDate) : IDomainEvent, INotification;
