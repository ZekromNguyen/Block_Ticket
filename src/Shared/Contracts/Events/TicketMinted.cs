namespace Shared.Contracts.Events;

public record TicketMinted(
    Guid TicketId,
    string TransactionHash,
    string TokenId,
    DateTime MintedAt,
    string ContractAddress = "",
    bool Success = true,
    string? FailureReason = null
);

public record TicketMintFailed(
    Guid TicketId,
    string Reason,
    DateTime FailedAt
);
