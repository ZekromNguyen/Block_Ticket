namespace Shared.Contracts.Dtos;

/// <summary>
/// Event analytics snapshot for promoter dashboards.
/// </summary>
public sealed record EventAnalyticsDto(
    Guid EventId,
    string EventName,
    DateTime GeneratedAt,
    int TotalTicketsSold,
    int TotalTicketsRefunded,
    int TotalTicketsCancelled,
    decimal TotalRevenue,
    decimal TotalRefundAmount,
    int TotalResales,
    decimal TotalResaleVolume,
    int TotalScans,
    int SuccessfulScans,
    int FailedScans,
    decimal AttendanceRate,
    IReadOnlyCollection<TicketTypeAnalytics> TicketTypeBreakdown,
    IReadOnlyCollection<DailySalesPoint> DailySales,
    IReadOnlyCollection<RevenueBySource> RevenueBreakdown);

/// <summary>
/// Analytics per ticket type.
/// </summary>
public sealed record TicketTypeAnalytics(
    Guid TicketTypeId,
    string TicketTypeName,
    int Sold,
    int Refunded,
    int OnResale,
    decimal Revenue,
    decimal AveragePrice);

/// <summary>
/// A single day's sales data point for charting.
/// </summary>
public sealed record DailySalesPoint(
    DateOnly Date,
    int TicketsSold,
    decimal Revenue,
    int Refunds);

/// <summary>
/// Revenue breakdown by source (primary, resale, etc.).
/// </summary>
public sealed record RevenueBySource(
    string Source,
    decimal Amount,
    int Count);

/// <summary>
/// Platform-wide summary analytics.
/// </summary>
public sealed record PlatformAnalyticsDto(
    DateTime GeneratedAt,
    int TotalEvents,
    int TotalUsers,
    int TotalTicketsSold,
    decimal TotalRevenue,
    int TotalResales,
    int TotalScans,
    decimal PlatformFeeRevenue,
    IReadOnlyCollection<TopEventAnalytics> TopEvents);

/// <summary>
/// Top-performing event summary.
/// </summary>
public sealed record TopEventAnalytics(
    Guid EventId,
    string EventName,
    int TicketsSold,
    decimal Revenue,
    decimal AttendanceRate);
