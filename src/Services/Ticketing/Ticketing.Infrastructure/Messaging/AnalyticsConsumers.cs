using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Contracts.Events;
using Ticketing.Infrastructure.Persistence;

namespace Ticketing.Infrastructure.Messaging;

/// <summary>
/// Projects TicketPurchased events into the EventAnalytics read model.
/// </summary>
public sealed class TicketPurchasedAnalyticsConsumer : IConsumer<TicketPurchased>
{
    private readonly TicketingDbContext _db;

    public TicketPurchasedAnalyticsConsumer(TicketingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TicketPurchased> context)
    {
        var msg = context.Message;
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == msg.TicketId, context.CancellationToken);
        if (ticket is null) return;

        var analytics = await _db.EventAnalytics.FirstOrDefaultAsync(a => a.EventId == msg.EventId, context.CancellationToken);
        if (analytics is null)
        {
            analytics = new Domain.Entities.EventAnalytics(msg.EventId, $"Event {msg.EventId}");
            _db.EventAnalytics.Add(analytics);
        }

        analytics.RecordSale(ticket.TicketTypeName, msg.Price, 1);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>
/// Projects TicketRefunded events into the EventAnalytics read model.
/// </summary>
public sealed class TicketRefundedAnalyticsConsumer : IConsumer<TicketRefunded>
{
    private readonly TicketingDbContext _db;

    public TicketRefundedAnalyticsConsumer(TicketingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TicketRefunded> context)
    {
        var msg = context.Message;
        var ticket = await _db.Tickets.FirstOrDefaultAsync(t => t.Id == msg.TicketId, context.CancellationToken);
        if (ticket is null) return;

        var analytics = await _db.EventAnalytics.FirstOrDefaultAsync(a => a.EventId == msg.EventId, context.CancellationToken);
        if (analytics is null)
        {
            analytics = new Domain.Entities.EventAnalytics(msg.EventId, $"Event {msg.EventId}");
            _db.EventAnalytics.Add(analytics);
        }

        analytics.RecordRefund(ticket.TicketTypeName, msg.Amount);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>
/// Projects TicketTransferred events into the EventAnalytics read model.
/// </summary>
public sealed class TicketTransferredAnalyticsConsumer : IConsumer<TicketTransferred>
{
    private readonly TicketingDbContext _db;

    public TicketTransferredAnalyticsConsumer(TicketingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TicketTransferred> context)
    {
        var msg = context.Message;
        var analytics = await _db.EventAnalytics.FirstOrDefaultAsync(a => a.EventId == msg.EventId, context.CancellationToken);
        if (analytics is null)
        {
            analytics = new Domain.Entities.EventAnalytics(msg.EventId, $"Event {msg.EventId}");
            _db.EventAnalytics.Add(analytics);
        }

        analytics.RecordResale(msg.Price);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}

/// <summary>
/// Projects TicketListedForResale events into the EventAnalytics read model.
/// </summary>
public sealed class TicketListedForResaleAnalyticsConsumer : IConsumer<TicketListedForResale>
{
    private readonly TicketingDbContext _db;

    public TicketListedForResaleAnalyticsConsumer(TicketingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TicketListedForResale> context)
    {
        var msg = context.Message;
        var analytics = await _db.EventAnalytics.FirstOrDefaultAsync(a => a.EventId == msg.EventId, context.CancellationToken);
        if (analytics is null)
        {
            analytics = new Domain.Entities.EventAnalytics(msg.EventId, $"Event {msg.EventId}");
            _db.EventAnalytics.Add(analytics);
        }

        analytics.RecordResale(msg.Price);
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
