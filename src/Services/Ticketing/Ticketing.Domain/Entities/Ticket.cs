using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

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
    public string? ContractAddress { get; private set; }
    public string? TokenId { get; private set; }
    public string? TransactionHash { get; private set; }
    public DateTime? MintedAt { get; private set; }
    public string VerificationCode { get; private set; } = string.Empty;
    public DateTime? UsedAt { get; private set; }
    public string? UsedBy { get; private set; }
    public string? UsedLocation { get; private set; }
    public bool IsResaleEligible { get; private set; } = true;
    public decimal? ResalePrice { get; private set; }
    public Guid? ResaleSellerUserId { get; private set; }
    public DateTime? ListedForResaleAt { get; private set; }
    public Guid? TransferredFromUserId { get; private set; }
    public DateTime? TransferredAt { get; private set; }
    public decimal? RefundedAmount { get; private set; }
    public string? RefundReason { get; private set; }
    public DateTime? RefundedAt { get; private set; }
    public bool VerificationOverrideAllowed { get; private set; }
    public string? VerificationOverrideReason { get; private set; }
    public DateTime? VerificationOverrideUntil { get; private set; }

    public Reservation Reservation { get; private set; } = null!;

    private Ticket()
    {
    }

    public Ticket(Guid reservationId, Guid reservationItemId, Guid userId, Guid eventId, Guid ticketTypeId, string ticketTypeName, decimal pricePaid)
    {
        TicketNumber = $"TKT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..29].ToUpperInvariant();
        ReservationId = reservationId;
        ReservationItemId = reservationItemId;
        UserId = userId;
        EventId = eventId;
        TicketTypeId = ticketTypeId;
        TicketTypeName = ticketTypeName;
        PricePaid = pricePaid;
        Status = TicketStatus.PendingMint;
        VerificationCode = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
    }

    public void MarkMinted(string contractAddress, string tokenId, string transactionHash, DateTime mintedAt)
    {
        if (Status is TicketStatus.Used or TicketStatus.Cancelled)
        {
            return;
        }

        ContractAddress = contractAddress;
        TokenId = tokenId;
        TransactionHash = transactionHash;
        MintedAt = mintedAt;
        Status = TicketStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkMintFailed(string reason)
    {
        if (Status != TicketStatus.PendingMint)
        {
            return;
        }

        Status = TicketStatus.MintFailed;
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = reason;
    }

    public void MarkUsed(string usedBy, string usedLocation)
    {
        if (Status != TicketStatus.Active && !CanUseWithOverride())
        {
            throw new InvalidOperationException("Only active tickets can be used");
        }

        Status = TicketStatus.Used;
        UsedAt = DateTime.UtcNow;
        UsedBy = usedBy;
        UsedLocation = usedLocation;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = TicketStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ListForResale(decimal price)
    {
        if (Status != TicketStatus.Active)
        {
            throw new InvalidOperationException("Only active tickets can be listed for resale");
        }

        if (!IsResaleEligible)
        {
            throw new InvalidOperationException("Ticket is not eligible for resale");
        }

        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Resale price must be greater than zero");
        }

        ResaleSellerUserId = UserId;
        ResalePrice = price;
        ListedForResaleAt = DateTime.UtcNow;
        Status = TicketStatus.OnResale;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CancelResale()
    {
        if (Status != TicketStatus.OnResale)
        {
            return;
        }

        ResalePrice = null;
        ResaleSellerUserId = null;
        ListedForResaleAt = null;
        Status = TicketStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransferTo(Guid buyerUserId)
    {
        if (Status != TicketStatus.OnResale)
        {
            throw new InvalidOperationException("Only resale tickets can be transferred through resale purchase");
        }

        if (buyerUserId == Guid.Empty || buyerUserId == UserId)
        {
            throw new ArgumentException("Buyer must be a different user", nameof(buyerUserId));
        }

        TransferredFromUserId = UserId;
        UserId = buyerUserId;
        TransferredAt = DateTime.UtcNow;
        ResalePrice = null;
        ResaleSellerUserId = null;
        ListedForResaleAt = null;
        Status = TicketStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Refund(decimal amount, string reason)
    {
        if (Status is TicketStatus.Refunded or TicketStatus.Used)
        {
            throw new InvalidOperationException("Ticket cannot be refunded");
        }

        RefundedAmount = amount;
        RefundReason = reason;
        RefundedAt = DateTime.UtcNow;
        IsResaleEligible = false;
        Status = TicketStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkRefunded(decimal amount, string reason)
    {
        RefundedAmount = amount;
        RefundReason = reason;
        RefundedAt = DateTime.UtcNow;
        IsResaleEligible = false;
        Status = TicketStatus.Refunded;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AllowVerificationOverride(string reason, DateTime? validUntil)
    {
        VerificationOverrideAllowed = true;
        VerificationOverrideReason = reason;
        VerificationOverrideUntil = validUntil;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearVerificationOverride()
    {
        VerificationOverrideAllowed = false;
        VerificationOverrideReason = null;
        VerificationOverrideUntil = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private bool CanUseWithOverride()
    {
        return VerificationOverrideAllowed &&
            (VerificationOverrideUntil is null || VerificationOverrideUntil > DateTime.UtcNow) &&
            Status is TicketStatus.MintFailed or TicketStatus.PendingMint or TicketStatus.Cancelled;
    }
}

public enum TicketStatus
{
    PendingMint = 1,
    Active = 2,
    Used = 3,
    Cancelled = 4,
    MintFailed = 5,
    OnResale = 6,
    Refunded = 7
}
