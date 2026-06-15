using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Ticketing.Application.Configuration;
using Ticketing.Application.DTOs;
using Ticketing.Application.Features.Reservations.Commands;
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
            return Task.FromResult(_reservations.SelectMany(reservation => reservation.Tickets).FirstOrDefault(ticket => ticket.Id == ticketId));
        }

        public Task<IReadOnlyCollection<Ticket>> GetTicketsByUserAsync(Guid userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyCollection<Ticket>>(_reservations.SelectMany(reservation => reservation.Tickets).Where(ticket => ticket.UserId == userId).ToList());
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
    }

    private sealed class RecordingPublisher : ITicketEventPublisher
    {
        public List<Guid> Purchased { get; } = new();

        public List<Guid> MintCommands { get; } = new();

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
    }
}
