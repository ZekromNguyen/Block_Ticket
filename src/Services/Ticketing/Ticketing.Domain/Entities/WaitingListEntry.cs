using Shared.Common.Models;

namespace Ticketing.Domain.Entities;

public class WaitingListEntry : BaseAuditableEntity
{
    public Guid UserId { get; private set; }
    public Guid EventId { get; private set; }
    public Guid TicketTypeId { get; private set; }
    public WaitingListStatus Status { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public DateTime? OfferedAt { get; private set; }
    public DateTime? OfferExpiresAt { get; private set; }

    private WaitingListEntry()
    {
    }

    public WaitingListEntry(Guid userId, Guid eventId, Guid ticketTypeId)
    {
        UserId = userId;
        EventId = eventId;
        TicketTypeId = ticketTypeId;
        Status = WaitingListStatus.Waiting;
        JoinedAt = DateTime.UtcNow;
    }

    public void CreateOffer(DateTime availableUntil)
    {
        Status = WaitingListStatus.Offered;
        OfferedAt = DateTime.UtcNow;
        OfferExpiresAt = availableUntil;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Leave()
    {
        Status = WaitingListStatus.Left;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAccepted()
    {
        Status = WaitingListStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkExpired()
    {
        if (Status == WaitingListStatus.Offered)
        {
            Status = WaitingListStatus.Expired;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

public enum WaitingListStatus
{
    Waiting = 1,
    Offered = 2,
    Accepted = 3,
    Expired = 4,
    Left = 5
}
