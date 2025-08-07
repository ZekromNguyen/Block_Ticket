using Shared.Common.Models;

namespace Ticketing.Api.Models;

public class Ticket : BaseAuditableEntity
{
    public Guid EventId { get; set; }
    public Guid UserId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Pending;
    public string? TransactionHash { get; set; }
    public string? TokenId { get; set; }
    public DateTime? MintedAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime? UsedAt { get; set; }
    
    public ICollection<TicketTransaction> Transactions { get; set; } = new List<TicketTransaction>();
}

public class TicketTransaction : BaseEntity
{
    public Guid TicketId { get; set; }
    public Ticket Ticket { get; set; } = null!;
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentTransactionId { get; set; }
    public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
    public string? FailureReason { get; set; }
}

public enum TicketStatus
{
    Pending,
    Paid,
    Minted,
    Used,
    Refunded,
    Cancelled
}

public enum TransactionType
{
    Purchase,
    Refund,
    Resale
}

public enum TransactionStatus
{
    Pending,
    Completed,
    Failed,
    Cancelled
}
