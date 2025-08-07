namespace Shared.Contracts.Commands;

public record MintTicketCommand(
    Guid TicketId,
    Guid EventId,
    string UserWalletAddress,
    decimal Price,
    string TicketMetadata
);
