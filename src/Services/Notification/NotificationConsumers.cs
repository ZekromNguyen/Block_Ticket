using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;

namespace Notification;

public sealed class TicketPurchasedNotificationConsumer : IConsumer<TicketPurchased>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TicketPurchasedNotificationConsumer> _logger;

    public TicketPurchasedNotificationConsumer(NotificationDbContext context, ILogger<TicketPurchasedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketPurchased> context)
    {
        await StoreMessageAsync(
            "TicketPurchased",
            context.Message.TicketId,
            $"user:{context.Message.UserId}",
            "Ticket purchase confirmed",
            $"Ticket {context.Message.TicketId} was purchased for event {context.Message.EventId}.",
            context.CancellationToken);
    }

    private async Task StoreMessageAsync(string type, Guid correlationId, string recipient, string subject, string body, CancellationToken cancellationToken)
    {
        var exists = await _context.Messages.AnyAsync(message => message.Type == type && message.CorrelationId == correlationId, cancellationToken);
        if (exists)
        {
            return;
        }

        _context.Messages.Add(new NotificationMessage
        {
            Type = type,
            CorrelationId = correlationId,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = "Sent",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Notification sent: {Subject} to {Recipient}", subject, recipient);
    }
}

public sealed class TicketMintedNotificationConsumer : IConsumer<TicketMinted>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TicketMintedNotificationConsumer> _logger;

    public TicketMintedNotificationConsumer(NotificationDbContext context, ILogger<TicketMintedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketMinted> context)
    {
        var type = context.Message.Success ? "TicketMinted" : "TicketMintFailed";
        var exists = await _context.Messages.AnyAsync(message => message.Type == type && message.CorrelationId == context.Message.TicketId, context.CancellationToken);
        if (exists)
        {
            return;
        }

        var subject = context.Message.Success ? "Ticket NFT minted" : "Ticket mint failed";
        var body = context.Message.Success
            ? $"Ticket {context.Message.TicketId} minted as token {context.Message.TokenId}."
            : $"Ticket {context.Message.TicketId} failed to mint: {context.Message.FailureReason}";

        _context.Messages.Add(new NotificationMessage
        {
            Type = type,
            CorrelationId = context.Message.TicketId,
            Recipient = $"ticket:{context.Message.TicketId}",
            Subject = subject,
            Body = body,
            Status = "Sent",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Notification sent: {Subject}", subject);
    }
}

public sealed class TicketMintFailedNotificationConsumer : IConsumer<TicketMintFailed>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TicketMintFailedNotificationConsumer> _logger;

    public TicketMintFailedNotificationConsumer(NotificationDbContext context, ILogger<TicketMintFailedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<TicketMintFailed> context)
    {
        var exists = await _context.Messages.AnyAsync(message => message.Type == "TicketMintFailed" && message.CorrelationId == context.Message.TicketId, context.CancellationToken);
        if (exists)
        {
            return;
        }

        _context.Messages.Add(new NotificationMessage
        {
            Type = "TicketMintFailed",
            CorrelationId = context.Message.TicketId,
            Recipient = $"ticket:{context.Message.TicketId}",
            Subject = "Ticket mint failed",
            Body = $"Ticket {context.Message.TicketId} failed to mint: {context.Message.Reason}",
            Status = "Sent",
            SentAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Notification sent: Ticket mint failed");
    }
}
