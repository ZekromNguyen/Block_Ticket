namespace Shared.Contracts.Events;

public record TicketMinted(
    Guid TicketId,
    string TransactionHash,
    string TokenId,
    DateTime MintedAt
);
