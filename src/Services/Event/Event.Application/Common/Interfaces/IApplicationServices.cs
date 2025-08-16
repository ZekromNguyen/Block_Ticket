using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Enums;

namespace Event.Application.Common.Interfaces;

/// <summary>
/// Application service for event management
/// </summary>
public interface IEventService
{
    Task<EventDto> CreateEventAsync(CreateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> UpdateEventAsync(Guid eventId, UpdateEventRequest request, CancellationToken cancellationToken = default);
    Task<EventDto?> GetEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDto?> GetEventBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default);
    Task<PagedResult<EventDto>> GetEventsAsync(GetEventsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EventDto>> SearchEventsAsync(SearchEventsRequest request, CancellationToken cancellationToken = default);
    Task<EventDto> PublishEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDto> CancelEventAsync(Guid eventId, string reason, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> IsSlugAvailableAsync(string slug, Guid organizationId, Guid? excludeEventId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for venue management
/// </summary>
public interface IVenueService
{
    Task<VenueDto> CreateVenueAsync(CreateVenueRequest request, CancellationToken cancellationToken = default);
    Task<VenueDto> UpdateVenueAsync(Guid venueId, UpdateVenueRequest request, CancellationToken cancellationToken = default);
    Task<VenueDto?> GetVenueAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<PagedResult<VenueDto>> GetVenuesAsync(GetVenuesRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<VenueDto>> SearchVenuesAsync(SearchVenuesRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteVenueAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<SeatMapDto?> GetSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default);
    Task<SeatMapDto> UpdateSeatMapAsync(Guid venueId, UpdateSeatMapRequest request, CancellationToken cancellationToken = default);
    Task<SeatMapImportResult> ImportSeatMapAsync(Guid venueId, ImportSeatMapRequest request, CancellationToken cancellationToken = default);
    Task<SeatMapExportResult> ExportSeatMapAsync(Guid venueId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for reservation management
/// </summary>
public interface IReservationService
{
    Task<ReservationDto> CreateReservationAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);
    Task<ReservationDto> ConfirmReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<ReservationDto> CancelReservationAsync(Guid reservationId, string reason, CancellationToken cancellationToken = default);
    Task<ReservationDto> ExtendReservationAsync(Guid reservationId, TimeSpan extension, CancellationToken cancellationToken = default);
    Task<ReservationDto?> GetReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);
    Task<ReservationDto?> GetReservationByNumberAsync(string reservationNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<ReservationDto>> GetUserReservationsAsync(Guid userId, GetReservationsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<ReservationDto>> GetEventReservationsAsync(Guid eventId, GetReservationsRequest request, CancellationToken cancellationToken = default);
    Task<int> CleanupExpiredReservationsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for ticket type management
/// </summary>
public interface ITicketTypeService
{
    Task<TicketTypeDto> CreateTicketTypeAsync(CreateTicketTypeRequest request, CancellationToken cancellationToken = default);
    Task<TicketTypeDto> UpdateTicketTypeAsync(Guid ticketTypeId, UpdateTicketTypeRequest request, CancellationToken cancellationToken = default);
    Task<TicketTypeDto?> GetTicketTypeAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TicketTypeDto>> GetEventTicketTypesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> DeleteTicketTypeAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<AvailabilityDto> GetAvailabilityAsync(Guid ticketTypeId, CancellationToken cancellationToken = default);
    Task<PricingDto> GetPricingAsync(Guid ticketTypeId, int quantity = 1, string? discountCode = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for pricing rule management
/// </summary>
public interface IPricingService
{
    Task<PricingRuleDto> CreatePricingRuleAsync(CreatePricingRuleRequest request, CancellationToken cancellationToken = default);
    Task<PricingRuleDto> UpdatePricingRuleAsync(Guid pricingRuleId, UpdatePricingRuleRequest request, CancellationToken cancellationToken = default);
    Task<PricingRuleDto?> GetPricingRuleAsync(Guid pricingRuleId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PricingRuleDto>> GetEventPricingRulesAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> DeletePricingRuleAsync(Guid pricingRuleId, CancellationToken cancellationToken = default);
    Task<PricingCalculationResult> CalculatePricingAsync(CalculatePricingRequest request, CancellationToken cancellationToken = default);
    Task<bool> ValidateDiscountCodeAsync(string discountCode, Guid eventId, Guid userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for allocation management
/// </summary>
public interface IAllocationService
{
    Task<AllocationDto> CreateAllocationAsync(CreateAllocationRequest request, CancellationToken cancellationToken = default);
    Task<AllocationDto> UpdateAllocationAsync(Guid allocationId, UpdateAllocationRequest request, CancellationToken cancellationToken = default);
    Task<AllocationDto?> GetAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<AllocationDto>> GetEventAllocationsAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> DeleteAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default);
    Task<bool> ValidateAccessCodeAsync(string accessCode, Guid eventId, Guid userId, CancellationToken cancellationToken = default);
    Task<AllocationAvailabilityDto> GetAllocationAvailabilityAsync(Guid allocationId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for event series management
/// </summary>
public interface IEventSeriesService
{
    Task<EventSeriesDto> CreateEventSeriesAsync(CreateEventSeriesRequest request, CancellationToken cancellationToken = default);
    Task<EventSeriesDto> UpdateEventSeriesAsync(Guid seriesId, UpdateEventSeriesRequest request, CancellationToken cancellationToken = default);
    Task<EventSeriesDto?> GetEventSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);
    Task<PagedResult<EventSeriesDto>> GetEventSeriesAsync(GetEventSeriesRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteEventSeriesAsync(Guid seriesId, CancellationToken cancellationToken = default);
    Task<EventSeriesDto> AddEventToSeriesAsync(Guid seriesId, Guid eventId, CancellationToken cancellationToken = default);
    Task<EventSeriesDto> RemoveEventFromSeriesAsync(Guid seriesId, Guid eventId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for availability and inventory management
/// </summary>
public interface IAvailabilityService
{
    Task<EventAvailabilityDto> GetEventAvailabilityAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<SeatAvailabilityDto> GetSeatAvailabilityAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<InventorySnapshotDto> GetInventorySnapshotAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<string> GetInventoryETagAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<bool> CheckSeatAvailabilityAsync(List<Guid> seatIds, CancellationToken cancellationToken = default);
    Task<ReservationValidationResult> ValidateReservationRequestAsync(CreateReservationRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for search and catalog
/// </summary>
public interface ICatalogService
{
    Task<PagedResult<EventCatalogDto>> GetPublicEventsAsync(GetPublicEventsRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<EventCatalogDto>> SearchPublicEventsAsync(SearchPublicEventsRequest request, CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetPublicEventDetailAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<EventDetailDto?> GetPublicEventDetailBySlugAsync(string slug, Guid organizationId, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventCatalogDto>> GetRecommendedEventsAsync(GetRecommendedEventsRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<EventCatalogDto>> GetUpcomingEventsAsync(int days = 30, CancellationToken cancellationToken = default);
    Task<IEnumerable<VenueCatalogDto>> GetPublicVenuesAsync(GetPublicVenuesRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Application service for reporting and analytics
/// </summary>
public interface IReportingService
{
    Task<EventSalesReportDto> GetEventSalesReportAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<VenueUtilizationReportDto> GetVenueUtilizationReportAsync(Guid venueId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<PromoterPerformanceReportDto> GetPromoterPerformanceReportAsync(Guid promoterId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<InventoryReportDto> GetInventoryReportAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task<ReservationMetricsDto> GetReservationMetricsAsync(Guid eventId, CancellationToken cancellationToken = default);
}
