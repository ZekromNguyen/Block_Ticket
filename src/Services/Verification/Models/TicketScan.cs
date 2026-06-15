namespace Verification.Models;

public sealed class TicketScan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TicketId { get; set; }
    public string VerificationCode { get; set; } = string.Empty;
    public string CheckedBy { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool Accepted { get; set; }
    public string Result { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}

public sealed record VerifyTicketRequest(Guid TicketId, string VerificationCode, string CheckedBy, string Location);

public sealed record VerifyTicketResponse(Guid TicketId, bool Accepted, string Reason, object? Ticket);

public sealed record ApiEnvelope<T>(bool Success, string Message, T? Data, IReadOnlyCollection<string> Errors);
