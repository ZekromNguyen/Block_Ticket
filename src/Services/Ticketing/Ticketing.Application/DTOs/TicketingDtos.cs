using Ticketing.Domain.Entities;

namespace Ticketing.Application.DTOs;

public sealed record ReservationItemRequest(Guid TicketTypeId, string TicketTypeName, decimal UnitPrice, int Quantity);

public sealed record CreateReservationRequest(
    Guid UserId,
    Guid EventId,
    string Currency,
    IReadOnlyCollection<ReservationItemRequest> Items,
    string? IdempotencyKey);

public sealed record ConfirmReservationRequest(Guid ReservationId, string PaymentMethod, string? UserWalletAddress);

public sealed record PurchaseTicketRequest(
    Guid EventId,
    Guid UserId,
    decimal Price,
    string PaymentMethod,
    string? UserWalletAddress,
    Guid? TicketTypeId,
    string? TicketTypeName,
    string? IdempotencyKey);

public sealed record VerifyTicketRequest(Guid TicketId, string VerificationCode, string CheckedBy, string Location);

public sealed record VerifyTicketResponse(Guid TicketId, bool Accepted, string Reason, TicketDto? Ticket);

public sealed record ReservationItemDto(Guid Id, Guid TicketTypeId, string TicketTypeName, decimal UnitPrice, int Quantity, decimal TotalPrice);

public sealed record TicketDto(
    Guid Id,
    Guid ReservationId,
    Guid EventId,
    Guid UserId,
    Guid TicketTypeId,
    string TicketTypeName,
    string TicketNumber,
    decimal PricePaid,
    TicketStatus Status,
    string? ContractAddress,
    string? TokenId,
    string? TransactionHash,
    DateTime? MintedAt,
    string VerificationCode,
    DateTime? UsedAt);

public sealed record ReservationDto(
    Guid Id,
    string ReservationNumber,
    Guid UserId,
    Guid EventId,
    ReservationStatus Status,
    DateTime ExpiresAt,
    decimal Subtotal,
    decimal ServiceFee,
    decimal ProcessingFee,
    decimal TotalAmount,
    string Currency,
    string? PaymentIntentId,
    IReadOnlyCollection<ReservationItemDto> Items,
    IReadOnlyCollection<TicketDto> Tickets);

public sealed record ConfirmReservationResponse(ReservationDto Reservation, IReadOnlyCollection<TicketDto> Tickets);

public sealed record PurchaseResponse
{
    public Guid TicketId { get; init; }
    public string TicketNumber { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? TransactionId { get; init; }
    public Guid ReservationId { get; init; }
}

public static class TicketingMappings
{
    public static ReservationDto ToDto(this Reservation reservation)
    {
        return new ReservationDto(
            reservation.Id,
            reservation.ReservationNumber,
            reservation.UserId,
            reservation.EventId,
            reservation.Status,
            reservation.ExpiresAt,
            reservation.Subtotal,
            reservation.ServiceFee,
            reservation.ProcessingFee,
            reservation.TotalAmount,
            reservation.Currency,
            reservation.PaymentIntentId,
            reservation.Items.Select(ToDto).ToList(),
            reservation.Tickets.Select(ToDto).ToList());
    }

    public static ReservationItemDto ToDto(this ReservationItem item)
    {
        return new ReservationItemDto(item.Id, item.TicketTypeId, item.TicketTypeName, item.UnitPrice, item.Quantity, item.TotalPrice);
    }

    public static TicketDto ToDto(this Ticket ticket)
    {
        return new TicketDto(
            ticket.Id,
            ticket.ReservationId,
            ticket.EventId,
            ticket.UserId,
            ticket.TicketTypeId,
            ticket.TicketTypeName,
            ticket.TicketNumber,
            ticket.PricePaid,
            ticket.Status,
            ticket.ContractAddress,
            ticket.TokenId,
            ticket.TransactionHash,
            ticket.MintedAt,
            ticket.VerificationCode,
            ticket.UsedAt);
    }
}
