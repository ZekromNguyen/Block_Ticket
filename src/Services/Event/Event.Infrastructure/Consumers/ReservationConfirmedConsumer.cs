using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using System.Linq;
using System.Threading.Tasks;

namespace Event.Infrastructure.Consumers;

public class ReservationConfirmedConsumer : IConsumer<ReservationConfirmedIntegrationEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ReservationConfirmedConsumer> _logger;

    public ReservationConfirmedConsumer(IUnitOfWork unitOfWork, ILogger<ReservationConfirmedConsumer> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ReservationConfirmedIntegrationEvent> context)
    {
        _logger.LogInformation("ReservationConfirmedIntegrationEvent received for reservation {ReservationId}", context.Message.ReservationId);

        var eventAggregate = await _unitOfWork.Events.GetByIdAsync(context.Message.EventId, context.CancellationToken);
        if (eventAggregate == null)
        {
            _logger.LogWarning("Event with ID {EventId} not found while processing reservation confirmation.", context.Message.EventId);
            return;
        }

        var venue = await _unitOfWork.Venues.GetWithSeatMapAsync(eventAggregate.VenueId, context.CancellationToken);
        if (venue == null)
        {
            _logger.LogWarning("Venue with ID {VenueId} not found for event {EventId} while processing reservation confirmation.", eventAggregate.VenueId, context.Message.EventId);
            return;
        }

        foreach (var seatId in context.Message.SeatIds)
        {
            var seat = venue.Seats.FirstOrDefault(s => s.Id == seatId);
            if (seat != null)
            {
                try
                {
                    seat.Sell();
                }
                catch (System.Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to sell seat {SeatId} for event {EventId}.", seat.Id, eventAggregate.Id);
                }
            }
            else
            {
                _logger.LogWarning("Seat with ID {SeatId} not found in venue {VenueId} for event {EventId} for reservation confirmation.", seatId, venue.Id, eventAggregate.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync(context.CancellationToken);
        _logger.LogInformation("Successfully processed ReservationConfirmedIntegrationEvent for reservation {ReservationId}", context.Message.ReservationId);
    }
}

