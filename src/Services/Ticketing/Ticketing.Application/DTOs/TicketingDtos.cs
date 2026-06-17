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

public sealed record ResaleListTicketRequest(Guid TicketId, Guid SellerUserId, decimal Price);

public sealed record ResalePurchaseTicketRequest(Guid TicketId, Guid BuyerUserId, string PaymentMethod);

public sealed record CancelResaleRequest(Guid TicketId, Guid SellerUserId, string Reason);

public sealed record WaitingListJoinRequest(Guid UserId, Guid EventId, Guid TicketTypeId);

public sealed record WaitingListOfferRequest(Guid EventId, Guid TicketTypeId, TimeSpan OfferTtl);

public sealed record WaitingListEntryDto(Guid Id, Guid UserId, Guid EventId, Guid TicketTypeId, WaitingListStatus Status, DateTime JoinedAt, DateTime? OfferExpiresAt);

public sealed record RefundTicketRequest(Guid TicketId, string RequestedBy, string Reason, string? UserWalletAddress);

public sealed record AdminForceExpireReservationRequest(Guid ReservationId, string AdminUserId, string Note);

public sealed record AdminRetryMintRequest(Guid TicketId, string UserWalletAddress, string AdminUserId, string Reason);

public sealed record AdminRefundTicketRequest(Guid TicketId, string AdminUserId, string Reason, string? UserWalletAddress);

public sealed record AdminVerificationOverrideRequest(Guid TicketId, string AdminUserId, string Reason, DateTime? ValidUntil);

public sealed record AdminAuditNoteDto(Guid Id, Guid? TicketId, Guid? ReservationId, string Action, string AdminUserId, string Note, DateTime CreatedAt);

public sealed record PaymentDto(
    Guid Id,
    Guid ReservationId,
    string PaymentIntentId,
    string PaymentMethod,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    string? TransactionId,
    string? FailureReason,
    DateTime? ProcessedAt);

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
    DateTime? UsedAt,
    bool IsResaleEligible,
    decimal? ResalePrice,
    Guid? ResaleSellerUserId,
    DateTime? ListedForResaleAt,
    Guid? TransferredFromUserId,
    DateTime? TransferredAt,
    decimal? RefundedAmount,
    string? RefundReason,
    DateTime? RefundedAt,
    bool VerificationOverrideAllowed,
    DateTime? VerificationOverrideUntil);

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
            ticket.UsedAt,
            ticket.IsResaleEligible,
            ticket.ResalePrice,
            ticket.ResaleSellerUserId,
            ticket.ListedForResaleAt,
            ticket.TransferredFromUserId,
            ticket.TransferredAt,
            ticket.RefundedAmount,
            ticket.RefundReason,
            ticket.RefundedAt,
            ticket.VerificationOverrideAllowed,
            ticket.VerificationOverrideUntil);
    }

    public static WaitingListEntryDto ToDto(this WaitingListEntry entry)
    {
        return new WaitingListEntryDto(entry.Id, entry.UserId, entry.EventId, entry.TicketTypeId, entry.Status, entry.JoinedAt, entry.OfferExpiresAt);
    }

    public static AdminAuditNoteDto ToDto(this AdminAuditNote note)
    {
        return new AdminAuditNoteDto(note.Id, note.TicketId, note.ReservationId, note.Action, note.AdminUserId, note.Note, note.CreatedAt);
    }

    public static PaymentDto ToDto(this ReservationPayment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.ReservationId,
            payment.PaymentIntentId,
            payment.PaymentMethod,
            payment.Amount,
            payment.Currency,
            payment.Status,
            payment.TransactionId,
            payment.FailureReason,
            payment.ProcessedAt);
    }
}
