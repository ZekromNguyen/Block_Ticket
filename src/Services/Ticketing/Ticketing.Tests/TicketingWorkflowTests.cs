using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Configuration;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Refunds.Commands;
using Ticketing.Application.Features.Reservations.Commands;
using Ticketing.Application.Features.Resale.Commands;
using Ticketing.Application.Features.WaitingList.Commands;
using Ticketing.Application.Interfaces;
using Ticketing.Domain.Entities;
using Xunit;

namespace Ticketing.Tests;

public sealed class TicketingWorkflowTests
{
    [Fact]
    public async Task CreateReservation_WithSameIdempotencyKey_ReturnsExistingReservation()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();
        var request = new CreateReservationRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "USD",
            new[] { new ReservationItemRequest(Guid.NewGuid(), "GA", 50m, 2) },
            "idem-1");

        var first = await mediator.Send(new CreateReservationCommand(request));
        var second = await mediator.Send(new CreateReservationCommand(request));

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.Equal(first.Value!.Id, second.Value!.Id);
    }

    [Fact]
    public async Task PurchaseTicket_WhenPaymentSucceeds_CreatesPendingMintTicketAndPublishesEvents()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();
        var publisher = (RecordingPublisher)provider.GetRequiredService<ITicketEventPublisher>();

        var result = await mediator.Send(new PurchaseTicketCommand(new PurchaseTicketRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            99m,
            "card",
            "wallet-1",
            Guid.NewGuid(),
            "GA",
            "purchase-1")));

        Assert.True(result.Succeeded);
        Assert.NotEqual(Guid.Empty, result.Value!.TicketId);
        Assert.Equal("PendingMint", result.Value.Status);
        Assert.Single(publisher.Purchased);
        Assert.Single(publisher.MintCommands);
    }

    [Fact]
    public async Task Resale_WhenTicketIsActive_TransfersOwnershipAndPublishesTransfer()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();
        var repository = (InMemoryRepository)provider.GetRequiredService<ITicketingRepository>();
        var publisher = (RecordingPublisher)provider.GetRequiredService<ITicketEventPublisher>();
        var sellerId = Guid.NewGuid();
        var buyerId = Guid.NewGuid();
        var ticket = repository.AddActiveTicket(sellerId);

        var listResult = await mediator.Send(new ListTicketForResaleCommand(new ResaleListTicketRequest(ticket.Id, sellerId, 80m)));
        var purchaseResult = await mediator.Send(new PurchaseResaleTicketCommand(new ResalePurchaseTicketRequest(ticket.Id, buyerId, "card")));

        Assert.True(listResult.Succeeded);
        Assert.True(purchaseResult.Succeeded);
        Assert.Equal(buyerId, purchaseResult.Value!.UserId);
        Assert.Single(publisher.Transferred);
    }

    [Fact]
    public async Task WaitingListOffer_WhenInventoryReturns_MarksNextUserOffered()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();
        var publisher = (RecordingPublisher)provider.GetRequiredService<ITicketEventPublisher>();
        var eventId = Guid.NewGuid();
        var ticketTypeId = Guid.NewGuid();

        await mediator.Send(new JoinWaitingListCommand(new WaitingListJoinRequest(Guid.NewGuid(), eventId, ticketTypeId)));
        var offer = await mediator.Send(new CreateWaitingListOfferCommand(new WaitingListOfferRequest(eventId, ticketTypeId, TimeSpan.FromMinutes(10))));

        Assert.True(offer.Succeeded);
        Assert.Equal(WaitingListStatus.Offered, offer.Value!.Status);
        Assert.Single(publisher.WaitingListOffers);
    }

    [Fact]
    public async Task RefundTicket_WhenWalletProvided_RestocksAndPublishesBurn()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();
        var repository = (InMemoryRepository)provider.GetRequiredService<ITicketingRepository>();
        var publisher = (RecordingPublisher)provider.GetRequiredService<ITicketEventPublisher>();
        var ticket = repository.AddActiveTicket(Guid.NewGuid());

        var result = await mediator.Send(new RefundTicketCommand(new RefundTicketRequest(ticket.Id, "user", "event cancelled", "wallet-1")));

        Assert.True(result.Succeeded);
        Assert.Equal(TicketStatus.Refunded, result.Value!.Status);
        Assert.Single(publisher.Refunded);
        Assert.Single(publisher.Restocked);
        Assert.Single(publisher.BurnCommands);
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddTicketingApplication();
        services.AddSingleton<ITicketingRepository, InMemoryRepository>();
        services.AddSingleton<IInventoryLockService, OpenInventoryLock>();
        services.AddSingleton<IPaymentProvider, PassingPaymentProvider>();
        services.AddSingleton<ITicketEventPublisher, RecordingPublisher>();
        return services.BuildServiceProvider();
    }

    private sealed class InMemoryRepository : ITicketingRepository
    {
        private readonly List<Reservation> _reservations = new();
        private readonly List<Ticket> _tickets = new();
        private readonly List<WaitingListEntry> _waitingListEntries = new();
        private readonly List<AdminAuditNote> _adminAuditNotes = new();

        public Ticket AddActiveTicket(Guid userId)
        {
            var reservationId = Guid.NewGuid();
            var reservationItemId = Guid.NewGuid();
            var ticket = new Ticket(reservationId, reservationItemId, userId, Guid.NewGuid(), Guid.NewGuid(), "GA", 50m);
            ticket.MarkMinted("0xcontract", "1", "0xtx", DateTime.UtcNow);
            _tickets.Add(ticket);
            return ticket;
        }

        public Task<Reservation?> GetReservationByIdAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservations.FirstOrDefault(reservation => reservation.Id == reservationId));
        }

        public Task<Reservation?> GetReservationByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservations.FirstOrDefault(reservation => reservation.IdempotencyKey == idempotencyKey));
        }

        public Task AddReservationAsync(Reservation reservation, CancellationToken cancellationToken)
        {
            _reservations.Add(reservation);
            return Task.CompletedTask;
        }

        public Task<Ticket?> GetTicketByIdAsync(Guid ticketId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_tickets.Concat(_reservations.SelectMany(reservation => reservation.Tickets)).FirstOrDefault(ticket => ticket.Id == ticketId));
        }

        public Task<IReadOnlyCollection<Ticket>> GetTicketsByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Ticket>>(_tickets.Concat(_reservations.SelectMany(reservation => reservation.Tickets)).Where(ticket => ticket.UserId == userId).ToList());
        }

        public Task<IReadOnlyCollection<Ticket>> GetRefundableTicketsByEventAsync(Guid eventId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Ticket>>(_tickets.Where(ticket => ticket.EventId == eventId && ticket.Status != TicketStatus.Refunded).ToList());
        }

        public Task<IReadOnlyCollection<Ticket>> GetResaleTicketsAsync(Guid? eventId, CancellationToken cancellationToken)
        {
            var query = _tickets.Where(ticket => ticket.Status == TicketStatus.OnResale);
            if (eventId.HasValue)
            {
                query = query.Where(ticket => ticket.EventId == eventId.Value);
            }

            return Task.FromResult<IReadOnlyCollection<Ticket>>(query.ToList());
        }

        public Task<WaitingListEntry?> GetWaitingListEntryAsync(Guid userId, Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_waitingListEntries.FirstOrDefault(entry => entry.UserId == userId && entry.EventId == eventId && entry.TicketTypeId == ticketTypeId));
        }

        public Task<IReadOnlyCollection<WaitingListEntry>> GetWaitingListEntriesAsync(Guid eventId, Guid ticketTypeId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<WaitingListEntry>>(_waitingListEntries.Where(entry => entry.EventId == eventId && entry.TicketTypeId == ticketTypeId).OrderBy(entry => entry.JoinedAt).ToList());
        }

        public Task AddWaitingListEntryAsync(WaitingListEntry entry, CancellationToken cancellationToken)
        {
            _waitingListEntries.Add(entry);
            return Task.CompletedTask;
        }

        public Task AddAdminAuditNoteAsync(AdminAuditNote note, CancellationToken cancellationToken)
        {
            _adminAuditNotes.Add(note);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<AdminAuditNote>> GetAdminAuditNotesAsync(Guid? ticketId, Guid? reservationId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<AdminAuditNote>>(_adminAuditNotes.ToList());
        }

        public Task<ReservationPayment?> GetPaymentByReservationIdAsync(Guid reservationId, CancellationToken cancellationToken)
        {
            return Task.FromResult(_reservations.FirstOrDefault(reservation => reservation.Id == reservationId)?.Payments.FirstOrDefault());
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class OpenInventoryLock : IInventoryLockService
    {
        public Task<bool> TryReserveAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, TimeSpan ttl, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public Task ReleaseAsync(Guid eventId, IReadOnlyCollection<ReservationItemRequest> items, string lockOwner, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class PassingPaymentProvider : IPaymentProvider
    {
        public Task<PaymentIntentResult> CreatePaymentIntentAsync(Guid reservationId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PaymentIntentResult($"pi_{reservationId:N}", true, null));
        }

        public Task<PaymentConfirmationResult> ConfirmPaymentAsync(string paymentIntentId, decimal amount, string currency, string paymentMethod, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PaymentConfirmationResult($"txn_{Guid.NewGuid():N}", true, null, "{}"));
        }

        public Task<PaymentRefundResult> RefundPaymentAsync(Guid ticketId, decimal amount, string reason, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PaymentRefundResult($"refund_{Guid.NewGuid():N}", true, null, "{}"));
        }
    }

    private sealed class RecordingPublisher : ITicketEventPublisher
    {
        public List<Guid> Purchased { get; } = new();

        public List<Guid> MintCommands { get; } = new();

        public List<Guid> Refunded { get; } = new();

        public List<Guid> Transferred { get; } = new();

        public List<Guid> WaitingListOffers { get; } = new();

        public List<Guid> BurnCommands { get; } = new();

        public List<Guid> Restocked { get; } = new();

        public Task PublishTicketPurchasedAsync(Ticket ticket, CancellationToken cancellationToken)
        {
            Purchased.Add(ticket.Id);
            return Task.CompletedTask;
        }

        public Task PublishMintTicketAsync(Ticket ticket, string userWalletAddress, CancellationToken cancellationToken)
        {
            MintCommands.Add(ticket.Id);
            return Task.CompletedTask;
        }

        public Task PublishTicketRefundedAsync(Ticket ticket, decimal amount, string reason, CancellationToken cancellationToken)
        {
            Refunded.Add(ticket.Id);
            return Task.CompletedTask;
        }

        public Task PublishTicketTransferredAsync(Ticket ticket, Guid fromUserId, Guid toUserId, decimal price, CancellationToken cancellationToken)
        {
            Transferred.Add(ticket.Id);
            return Task.CompletedTask;
        }

        public Task PublishTicketListedForResaleAsync(Ticket ticket, decimal price, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishResaleListingCancelledAsync(Ticket ticket, string reason, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishWaitingListOfferAsync(WaitingListEntry entry, CancellationToken cancellationToken)
        {
            WaitingListOffers.Add(entry.Id);
            return Task.CompletedTask;
        }

        public Task PublishBurnTicketAsync(Ticket ticket, string userWalletAddress, string reason, CancellationToken cancellationToken)
        {
            BurnCommands.Add(ticket.Id);
            return Task.CompletedTask;
        }

        public Task PublishRetryMintAsync(Ticket ticket, string userWalletAddress, string requestedBy, string reason, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishReservationReleasedAsync(Reservation reservation, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task PublishTicketsRestockedAsync(Ticket ticket, string reason, CancellationToken cancellationToken)
        {
            Restocked.Add(ticket.Id);
            return Task.CompletedTask;
        }
    }
}
