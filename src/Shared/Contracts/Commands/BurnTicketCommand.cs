namespace Shared.Contracts.Commands;

public record BurnTicketCommand(
    Guid TicketId,
    string UserWalletAddress,
    string Reason
);
