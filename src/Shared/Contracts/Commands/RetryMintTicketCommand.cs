namespace Shared.Contracts.Commands;

public record RetryMintTicketCommand(
    Guid TicketId,
    string UserWalletAddress,
    string RequestedBy,
    string Reason
);
