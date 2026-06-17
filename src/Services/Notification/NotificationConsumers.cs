using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using System.Security.Cryptography;
using System.Text;

namespace Notification;

public abstract class NotificationConsumerBase
{
    protected static async Task StoreMessageAsync(
        NotificationDbContext context,
        ILogger logger,
        string type,
        Guid correlationId,
        string recipient,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var exists = await context.Messages.AnyAsync(message => message.Type == type && message.CorrelationId == correlationId, cancellationToken);
        if (exists)
        {
            return;
        }

        context.Messages.Add(new NotificationMessage
        {
            Type = type,
            CorrelationId = correlationId,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            Status = "Sent",
            SentAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Notification sent: {Subject} to {Recipient}", subject, recipient);
    }

    protected static Guid CreateDeterministicId(Guid namespaceId, string name)
    {
        var input = Encoding.UTF8.GetBytes($"{namespaceId:N}:{name}");
        var hash = SHA256.HashData(input);
        return new Guid(hash[..16]);
    }
}

public sealed class TicketPurchasedNotificationConsumer : NotificationConsumerBase, IConsumer<TicketPurchased>
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
            _context,
            _logger,
            "TicketPurchased",
            context.Message.TicketId,
            $"user:{context.Message.UserId}",
            "Ticket purchase confirmed",
            $"Ticket {context.Message.TicketId} was purchased for event {context.Message.EventId}.",
            context.CancellationToken);
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

public sealed class TicketRefundedNotificationConsumer : NotificationConsumerBase, IConsumer<TicketRefunded>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TicketRefundedNotificationConsumer> _logger;

    public TicketRefundedNotificationConsumer(NotificationDbContext context, ILogger<TicketRefundedNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TicketRefunded> context)
    {
        return StoreMessageAsync(
            _context,
            _logger,
            "TicketRefunded",
            context.Message.TicketId,
            $"user:{context.Message.UserId}",
            "Ticket refunded",
            $"Ticket {context.Message.TicketId} was refunded for {context.Message.Amount}.",
            context.CancellationToken);
    }
}

public sealed class WaitingListOfferNotificationConsumer : NotificationConsumerBase, IConsumer<YourTurnInWaitingList>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<WaitingListOfferNotificationConsumer> _logger;

    public WaitingListOfferNotificationConsumer(NotificationDbContext context, ILogger<WaitingListOfferNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<YourTurnInWaitingList> context)
    {
        var correlationId = CreateDeterministicId(context.Message.EventId, $"{context.Message.UserId}:{context.Message.AvailableUntil:O}");
        return StoreMessageAsync(
            _context,
            _logger,
            "YourTurnInWaitingList",
            correlationId,
            $"user:{context.Message.UserId}",
            "Ticket available from waiting list",
            $"A ticket is available until {context.Message.AvailableUntil:O}.",
            context.CancellationToken);
    }
}

public sealed class TicketTransferredNotificationConsumer : NotificationConsumerBase, IConsumer<TicketTransferred>
{
    private readonly NotificationDbContext _context;
    private readonly ILogger<TicketTransferredNotificationConsumer> _logger;

    public TicketTransferredNotificationConsumer(NotificationDbContext context, ILogger<TicketTransferredNotificationConsumer> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<TicketTransferred> context)
    {
        return StoreMessageAsync(
            _context,
            _logger,
            "TicketTransferred",
            context.Message.TicketId,
            $"user:{context.Message.ToUserId}",
            "Resale ticket transferred",
            $"Ticket {context.Message.TicketId} was transferred to your account.",
            context.CancellationToken);
    }
}
