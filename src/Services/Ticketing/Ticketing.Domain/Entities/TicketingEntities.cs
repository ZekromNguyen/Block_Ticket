using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

/// <summary>
/// Represents a ticket reservation made by a user
/// This is the core entity for your workflow: Promoter → Event → User Reservation
/// </summary>
public class Reservation : BaseAuditableEntity
{
    public string ReservationNumber { get; private set; } = string.Empty;
    public Guid UserId { get; private set; } // Fan from Identity Service
    public Guid EventId { get; private set; } // From Event Service
    public ReservationStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal ServiceFee { get; private set; }
    public decimal ProcessingFee { get; private set; }
    public decimal TotalPaid { get; private set; }
    public string? PaymentIntentId { get; private set; } // For payment processing
    public string? FailureReason { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    
    // Metadata for tracking
    public string UserEmail { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string EventName { get; private set; } = string.Empty;
    public DateTime EventDate { get; private set; }
    public string IpAddress { get; private set; } = string.Empty;
    public string UserAgent { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<ReservationItem> Items { get; set; } = new List<ReservationItem>();
    public ICollection<ReservationPayment> Payments { get; set; } = new List<ReservationPayment>();

    private Reservation() { } // For EF Core

    public Reservation(Guid userId, Guid eventId, string userEmail, string userName,
                      string eventName, DateTime eventDate, string ipAddress, string userAgent,
                      int reservationExpiryMinutes = 15)
    {
        ReservationNumber = GenerateReservationNumber();
        UserId = userId;
        EventId = eventId;
        UserEmail = userEmail;
        UserName = userName;
        EventName = eventName;
        EventDate = eventDate;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        Status = ReservationStatus.Pending;
        ExpiresAt = DateTime.UtcNow.AddMinutes(reservationExpiryMinutes);
        TotalAmount = 0;
        ServiceFee = 0;
        ProcessingFee = 0;
        TotalPaid = 0;
    }

    public void AddReservationItem(Guid ticketTypeId, string ticketTypeName, decimal unitPrice, int quantity)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Cannot modify confirmed or cancelled reservation");

        var existingItem = Items.FirstOrDefault(i => i.TicketTypeId == ticketTypeId);
        if (existingItem != null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
        }
        else
        {
            var item = new ReservationItem(Id, ticketTypeId, ticketTypeName, unitPrice, quantity);
            Items.Add(item);
        }

        RecalculateAmounts();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateItemQuantity(Guid ticketTypeId, int newQuantity)
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Cannot modify confirmed or cancelled reservation");

        var item = Items.FirstOrDefault(i => i.TicketTypeId == ticketTypeId);
        if (item == null)
            throw new ArgumentException("Ticket type not found in reservation");

        if (newQuantity <= 0)
        {
            Items.Remove(item);
        }
        else
        {
            item.UpdateQuantity(newQuantity);
        }

        RecalculateAmounts();
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPaymentIntent(string paymentIntentId)
    {
        PaymentIntentId = paymentIntentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfirmReservation()
    {
        if (Status != ReservationStatus.Pending)
            throw new InvalidOperationException("Only pending reservations can be confirmed");

        if (DateTime.UtcNow > ExpiresAt)
            throw new InvalidOperationException("Reservation has expired");

        Status = ReservationStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CancelReservation(string reason)
    {
        if (Status == ReservationStatus.Cancelled)
            return;

        Status = ReservationStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        FailureReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsExpired()
    {
        if (Status == ReservationStatus.Pending && DateTime.UtcNow > ExpiresAt)
        {
            Status = ReservationStatus.Expired;
            FailureReason = "Reservation expired";
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public bool IsExpired => Status == ReservationStatus.Pending && DateTime.UtcNow > ExpiresAt;

    private void RecalculateAmounts()
    {
        var subtotal = Items.Sum(i => i.TotalPrice);
        ServiceFee = CalculateServiceFee(subtotal);
        ProcessingFee = CalculateProcessingFee(subtotal);
        TotalAmount = subtotal + ServiceFee + ProcessingFee;
    }

    private decimal CalculateServiceFee(decimal subtotal)
    {
        // 3% service fee with minimum $2, maximum $25
        var fee = subtotal * 0.03m;
        return Math.Max(2m, Math.Min(25m, fee));
    }

    private decimal CalculateProcessingFee(decimal subtotal)
    {
        // $1.50 flat processing fee
        return 1.50m;
    }

    private string GenerateReservationNumber()
    {
        return $"RES-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

/// <summary>
/// Represents individual ticket types and quantities within a reservation
/// Links to Event Service ticket types
/// </summary>
public class ReservationItem : BaseAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public Guid TicketTypeId { get; private set; } // From Event Service
    public string TicketTypeName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal TotalPrice { get; private set; }

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;

    private ReservationItem() { } // For EF Core

    public ReservationItem(Guid reservationId, Guid ticketTypeId, string ticketTypeName, 
                          decimal unitPrice, int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero");

        ReservationId = reservationId;
        TicketTypeId = ticketTypeId;
        TicketTypeName = ticketTypeName;
        UnitPrice = unitPrice;
        Quantity = quantity;
        TotalPrice = unitPrice * quantity;
    }

    public void UpdateQuantity(int newQuantity)
    {
        if (newQuantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero");

        Quantity = newQuantity;
        TotalPrice = UnitPrice * Quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdatePrice(decimal newUnitPrice)
    {
        UnitPrice = newUnitPrice;
        TotalPrice = UnitPrice * Quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Tracks payment attempts and status for reservations
/// </summary>
public class ReservationPayment : BaseAuditableEntity
{
    public Guid ReservationId { get; private set; }
    public string PaymentIntentId { get; private set; } = string.Empty;
    public string PaymentMethodId { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public PaymentStatus Status { get; private set; }
    public PaymentMethod Method { get; private set; }
    public string? FailureReason { get; private set; }
    public string? TransactionId { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string ProcessorData { get; private set; } = "{}"; // JSON metadata from payment processor

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;

    private ReservationPayment() { } // For EF Core

    public ReservationPayment(Guid reservationId, string paymentIntentId, string paymentMethodId,
                             decimal amount, string currency, PaymentMethod method)
    {
        ReservationId = reservationId;
        PaymentIntentId = paymentIntentId;
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = PaymentStatus.Pending;
    }

    public void MarkAsSucceeded(string transactionId, string processorData)
    {
        Status = PaymentStatus.Succeeded;
        TransactionId = transactionId;
        ProcessorData = processorData;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string failureReason, string processorData)
    {
        Status = PaymentStatus.Failed;
        FailureReason = failureReason;
        ProcessorData = processorData;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsRefunded(string transactionId, string processorData)
    {
        Status = PaymentStatus.Refunded;
        TransactionId = transactionId;
        ProcessorData = processorData;
        ProcessedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the actual tickets generated after successful payment
/// These will be minted on blockchain via Blockchain Orchestrator
/// </summary>
public class Ticket : BaseAuditableEntity
{
    public string TicketNumber { get; private set; } = string.Empty;
    public Guid ReservationId { get; private set; }
    public Guid ReservationItemId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid TicketTypeId { get; private set; }
    public string TicketTypeName { get; private set; } = string.Empty;
    public decimal PricePaid { get; private set; }
    public TicketStatus Status { get; private set; }
    
    // Blockchain information
    public string? ContractAddress { get; private set; }
    public string? TokenId { get; private set; }
    public string? TransactionHash { get; private set; }
    public DateTime? MintedAt { get; private set; }
    
    // Verification information
    public string? VerificationCode { get; private set; }
    public DateTime? UsedAt { get; private set; }
    public string? UsedBy { get; private set; } // Staff member who verified
    public string? UsedLocation { get; private set; }
    
    // Transfer/Resale information
    public Guid? TransferredFromUserId { get; private set; }
    public DateTime? TransferredAt { get; private set; }
    public bool IsResaleEligible { get; private set; }

    // Navigation properties
    public Reservation Reservation { get; set; } = null!;

    private Ticket() { } // For EF Core

    public Ticket(Guid reservationId, Guid reservationItemId, Guid userId, Guid eventId,
                 Guid ticketTypeId, string ticketTypeName, decimal pricePaid)
    {
        TicketNumber = GenerateTicketNumber();
        ReservationId = reservationId;
        ReservationItemId = reservationItemId;
        UserId = userId;
        EventId = eventId;
        TicketTypeId = ticketTypeId;
        TicketTypeName = ticketTypeName;
        PricePaid = pricePaid;
        Status = TicketStatus.PendingMint;
        VerificationCode = GenerateVerificationCode();
        IsResaleEligible = true;
    }

    public void MarkAsMinted(string contractAddress, string tokenId, string transactionHash)
    {
        ContractAddress = contractAddress;
        TokenId = tokenId;
        TransactionHash = transactionHash;
        MintedAt = DateTime.UtcNow;
        Status = TicketStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsUsed(string usedBy, string usedLocation)
    {
        if (Status != TicketStatus.Active)
            throw new InvalidOperationException("Only active tickets can be used");

        Status = TicketStatus.Used;
        UsedAt = DateTime.UtcNow;
        UsedBy = usedBy;
        UsedLocation = usedLocation;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransferTo(Guid newUserId)
    {
        if (Status != TicketStatus.Active)
            throw new InvalidOperationException("Only active tickets can be transferred");

        TransferredFromUserId = UserId;
        UserId = newUserId;
        TransferredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = TicketStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    private string GenerateTicketNumber()
    {
        return $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..12].ToUpper()}";
    }

    private string GenerateVerificationCode()
    {
        return Guid.NewGuid().ToString("N")[..8].ToUpper();
    }
}

// Enums
public enum ReservationStatus
{
    Pending = 1,
    Confirmed = 2,
    Cancelled = 3,
    Expired = 4
}

public enum PaymentStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    Refunded = 4,
    Cancelled = 5
}

public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    BankTransfer = 3,
    DigitalWallet = 4,
    Cryptocurrency = 5
}

public enum TicketStatus
{
    PendingMint = 1,    // Waiting to be minted on blockchain
    Active = 2,         // Minted and ready to use
    Used = 3,           // Already used for entry
    Cancelled = 4,      // Cancelled/refunded
    Transferred = 5,    // Transferred to another user
    OnResale = 6        // Listed for resale
}
