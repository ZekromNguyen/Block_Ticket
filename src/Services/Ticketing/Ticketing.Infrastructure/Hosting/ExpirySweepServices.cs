using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Ticketing.Application.Interfaces;

namespace Ticketing.Infrastructure.Hosting;

public sealed class WaitingListExpirySweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WaitingListExpirySweepService> _logger;

    public WaitingListExpirySweepService(IServiceScopeFactory scopeFactory, ILogger<WaitingListExpirySweepService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITicketingRepository>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var expired = await repository.GetExpiredWaitingListEntriesAsync(stoppingToken);
                foreach (var entry in expired)
                {
                    entry.MarkExpired();
                    _logger.LogInformation(
                        "Waiting list offer expired for user {UserId}, event {EventId}, ticket type {TicketTypeId}",
                        entry.UserId, entry.EventId, entry.TicketTypeId);
                }

                if (expired.Count > 0)
                {
                    await repository.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during waiting list expiry sweep");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}

public sealed class ReservationExpirySweepService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationExpirySweepService> _logger;

    public ReservationExpirySweepService(IServiceScopeFactory scopeFactory, ILogger<ReservationExpirySweepService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<ITicketingRepository>();

                var expiredReservations = await repository.GetExpiredReservationsAsync(stoppingToken);
                foreach (var reservation in expiredReservations)
                {
                    reservation.MarkExpired();
                    _logger.LogInformation(
                        "Reservation {ReservationId} expired for user {UserId}",
                        reservation.Id, reservation.UserId);
                }

                if (expiredReservations.Count > 0)
                {
                    await repository.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during reservation expiry sweep");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
