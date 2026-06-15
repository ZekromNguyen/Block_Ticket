using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

public class ReservationPayment : BaseAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public string PaymentIntentId { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public string? FailureReason { get; private set; }
    public string? TransactionId { get; private set; }
    public string ProcessorData { get; private set; } = "{}";
    public DateTime? ProcessedAt { get; private set; }

    public Reservation Reservation { get; private set; } = null!;

    private ReservationPayment()
    {
    }

    public ReservationPayment(Guid reservationId, string paymentIntentId, string paymentMethod, decimal amount, string currency)
    {
        ReservationId = reservationId;
        PaymentIntentId = paymentIntentId;
        PaymentMethod = paymentMethod;
        Amount = amount;
        Currency = currency;
        Status = PaymentStatus.Pending;
    }

    public void MarkSucceeded(string transactionId, string processorData)
    {
        Status = PaymentStatus.Succeeded;
        TransactionId = transactionId;
        ProcessorData = processorData;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason, string processorData)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProcessorData = processorData;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}
