using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Messaging;

public sealed class TicketMintedConsumer : IConsumer<TicketMinted>
{
    private readonly TicketingDbContext _context;
    private readonly ILogger<TicketMintedConsumer> _logger;

    public TicketMintedConsumer(TicketingDbContext context, ILogger<TicketMintedConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketMinted> context)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { context.Message.TicketId }, context.CancellationToken);
        if (ticket is null)
        {
            _logger.LogWarning("Received mint result for missing ticket {TicketId}", context.Message.TicketId);
            return;
        }

        if (!context.Message.Success)
        {
            ticket.MarkMintFailed(context.Message.FailureReason ?? "Mint failed");
        }
        else
        {
            ticket.MarkMinted(
                context.Message.ContractAddress,
                context.Message.TokenId,
                context.Message.TransactionHash,
                context.Message.MintedAt);
        }

        await _context.SaveChangesAsync(context.CancellationToken);
    }
}

public sealed class TicketMintFailedConsumer : IConsumer<TicketMintFailed>
{
    private readonly TicketingDbContext _context;

    public TicketMintFailedConsumer(TicketingDbContext context)
    {
        _context = context;
    }

    public async Task Consume(ConsumeContext<TicketMintFailed> context)
    {
        var ticket = await _context.Tickets.FindAsync(new object[] { context.Message.TicketId }, context.CancellationToken);
        if (ticket is null)
        {
            return;
        }

        ticket.MarkMintFailed(context.Message.Reason);
        await _context.SaveChangesAsync(context.CancellationToken);
    }
}
