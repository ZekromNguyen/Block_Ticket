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
        if (Status != TicketStatus.Active)
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
}

public enum TicketStatus
{
    PendingMint = 1,
    Active = 2,
    Used = 3,
    Cancelled = 4,
    MintFailed = 5
}
