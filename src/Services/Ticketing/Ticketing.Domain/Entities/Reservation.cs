using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

public class Reservation : BaseAuditableEntity
{
    public string ReservationNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal DiscountTotal { get; private set; }
    public decimal ServiceFee { get; private set; }
    public decimal ProcessingFee { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public string? PricingSnapshotJson { get; private set; }
    public string? IdempotencyKey { get; private set; }
    public string InventoryLockOwner { get; private set; } = string.Empty;
    public string? PaymentIntentId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    public ICollection<ReservationItem> Items { get; private set; } = new List<ReservationItem>();
    public ICollection<ReservationPayment> Payments { get; private set; } = new List<ReservationPayment>();
    public ICollection<Ticket> Tickets { get; private set; } = new List<Ticket>();

    private Reservation()
    {
    }

    public Reservation(Guid userId, Guid eventId, string currency, string? idempotencyKey, string inventoryLockOwner, DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User id is required", nameof(userId));
        }

        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("Event id is required", nameof(eventId));
        }

        ReservationNumber = $"RES-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..25].ToUpperInvariant();
        UserId = userId;
        EventId = eventId;
        Currency = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.ToUpperInvariant();
        IdempotencyKey = string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey;
        InventoryLockOwner = inventoryLockOwner;
        ExpiresAt = expiresAt;
        Status = ReservationStatus.Pending;
    }

    public void AddItem(Guid ticketTypeId, string ticketTypeName, decimal unitPrice, int quantity)
    {
        EnsurePending();

        var existing = Items.FirstOrDefault(item => item.TicketTypeId == ticketTypeId);
        if (existing is not null)
        {
            existing.UpdateQuantity(existing.Quantity + quantity);
        }
        else
        {
            Items.Add(new ReservationItem(Id, ticketTypeId, ticketTypeName, unitPrice, quantity));
        }

        Recalculate();
    }

    public void SetPaymentIntent(string paymentIntentId)
    {
        EnsurePending();
        PaymentIntentId = paymentIntentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPricingSnapshot(decimal subtotal, decimal discountTotal, decimal serviceFee, decimal processingFee, decimal totalAmount)
    {
        EnsurePending();
        Subtotal = subtotal;
        DiscountTotal = discountTotal;
        ServiceFee = serviceFee;
        ProcessingFee = processingFee;
        TotalAmount = totalAmount;
        PricingSnapshotJson = System.Text.Json.JsonSerializer.Serialize(new
        {
            Subtotal = subtotal,
            DiscountTotal = discountTotal,
            ServiceFee = serviceFee,
            ProcessingFee = processingFee,
            TotalAmount = totalAmount
        });
        UpdatedAt = DateTime.UtcNow;
    }

    public ReservationPayment AddPayment(string paymentIntentId, string paymentMethod, decimal amount, string currency)
    {
        EnsurePending();

        var payment = new ReservationPayment(Id, paymentIntentId, paymentMethod, amount, currency);
        Payments.Add(payment);
        PaymentIntentId = paymentIntentId;
        UpdatedAt = DateTime.UtcNow;

        return payment;
    }

    public IReadOnlyCollection<Ticket> Confirm(string paymentTransactionId, string processorData)
    {
        EnsurePending();

        if (DateTime.UtcNow > ExpiresAt)
        {
            MarkExpired();
            throw new InvalidOperationException("Reservation has expired");
        }

        var payment = Payments.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        payment?.MarkSucceeded(paymentTransactionId, processorData);

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        foreach (var item in Items)
        {
            for (var index = 0; index < item.Quantity; index++)
            {
                Tickets.Add(new Ticket(Id, item.Id, UserId, EventId, item.TicketTypeId, item.TicketTypeName, item.UnitPrice));
            }
        }

        return Tickets.ToList();
    }

    public void Cancel(string reason)
    {
        if (Status == ReservationStatus.Cancelled)
        {
            return;
        }

        Status = ReservationStatus.Cancelled;
        FailureReason = reason;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status != ReservationStatus.Pending)
        {
            return;
        }

        Status = ReservationStatus.Expired;
        FailureReason = "Reservation expired";
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsExpired(DateTime utcNow) => Status == ReservationStatus.Pending && utcNow > ExpiresAt;

    private void EnsurePending()
    {
        if (Status != ReservationStatus.Pending)
        {
            throw new InvalidOperationException("Only pending reservations can be changed");
        }
    }

    private void Recalculate()
    {
        Subtotal = Items.Sum(item => item.TotalPrice);
        ServiceFee = Math.Round(Math.Max(0m, Subtotal * 0.03m), 2);
        ProcessingFee = Items.Count == 0 ? 0m : 1.50m;
        TotalAmount = Subtotal + ServiceFee + ProcessingFee;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}
