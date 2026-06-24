namespace Shared.Contracts.Dtos;

public enum SeatAvailability
{
    Available = 1,
    Held = 2,
    Sold = 3,
    Blocked = 4
}

public sealed record SeatDto(
    Guid SeatId,
    string Section,
    string Row,
    string Number,
    decimal Price,
    string Currency,
    SeatAvailability Availability);

public sealed record SeatAvailabilitySnapshotDto(
    Guid EventId,
    Guid TicketTypeId,
    IReadOnlyCollection<SeatDto> Seats,
    DateTime GeneratedAt);

public sealed record SeatHoldRequestDto(
    Guid EventId,
    Guid TicketTypeId,
    IReadOnlyCollection<Guid> SeatIds,
    string HoldOwner,
    DateTime ExpiresAt);

public sealed record SeatHoldResponseDto(
    string HoldId,
    Guid EventId,
    IReadOnlyCollection<Guid> HeldSeatIds,
    IReadOnlyCollection<Guid> RejectedSeatIds,
    DateTime ExpiresAt);

public sealed record ResalePolicyDto(
    Guid EventId,
    bool AllowResale,
    decimal? MaxResalePercent,
    decimal? MinResalePrice,
    decimal? MaxResalePrice);