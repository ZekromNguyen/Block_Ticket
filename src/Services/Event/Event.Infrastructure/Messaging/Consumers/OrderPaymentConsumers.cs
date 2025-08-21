using Event.Application.IntegrationEvents.Events;
using Event.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Event.Infrastructure.Messaging.Consumers;

/// <summary>
/// Consumer for order payment authorized events from Ticketing Service
/// </summary>
public class OrderPaymentAuthorizedConsumer : IConsumer<OrderPaymentAuthorizedIntegrationEvent>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<OrderPaymentAuthorizedConsumer> _logger;

    public OrderPaymentAuthorizedConsumer(
        IReservationRepository reservationRepository,
        ILogger<OrderPaymentAuthorizedConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPaymentAuthorizedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderPaymentAuthorized for Reservation {ReservationId}, Order {OrderId}", 
            message.ReservationId, message.OrderId);

        try
        {
            // Get the reservation
            var reservation = await _reservationRepository.GetByIdAsync(message.ReservationId, context.CancellationToken);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found for payment authorization", message.ReservationId);
                return;
            }

            // Mark payment as authorized (extend expiry time to allow for payment completion)
            reservation.ExtendExpiry(TimeSpan.FromMinutes(30));
            
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Successfully processed OrderPaymentAuthorized for Reservation {ReservationId}", 
                message.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderPaymentAuthorized for Reservation {ReservationId}", 
                message.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Consumer for order payment completed events from Ticketing Service
/// </summary>
public class OrderPaymentCompletedConsumer : IConsumer<OrderPaymentCompletedIntegrationEvent>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<OrderPaymentCompletedConsumer> _logger;

    public OrderPaymentCompletedConsumer(
        IReservationRepository reservationRepository,
        ILogger<OrderPaymentCompletedConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPaymentCompletedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderPaymentCompleted for Reservation {ReservationId}, Order {OrderId}", 
            message.ReservationId, message.OrderId);

        try
        {
            // Get the reservation
            var reservation = await _reservationRepository.GetByIdAsync(message.ReservationId, context.CancellationToken);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found for payment completion", message.ReservationId);
                return;
            }

            // Confirm the reservation
            reservation.Confirm(message.PaymentReference);
            
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Successfully confirmed Reservation {ReservationId} after payment completion", 
                message.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderPaymentCompleted for Reservation {ReservationId}", 
                message.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Consumer for order payment failed events from Ticketing Service
/// </summary>
public class OrderPaymentFailedConsumer : IConsumer<OrderPaymentFailedIntegrationEvent>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly ILogger<OrderPaymentFailedConsumer> _logger;

    public OrderPaymentFailedConsumer(
        IReservationRepository reservationRepository,
        ILogger<OrderPaymentFailedConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPaymentFailedIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderPaymentFailed for Reservation {ReservationId}, Order {OrderId}", 
            message.ReservationId, message.OrderId);

        try
        {
            // Get the reservation
            var reservation = await _reservationRepository.GetByIdAsync(message.ReservationId, context.CancellationToken);
            if (reservation == null)
            {
                _logger.LogWarning("Reservation {ReservationId} not found for payment failure", message.ReservationId);
                return;
            }

            // Cancel the reservation due to payment failure
            reservation.Cancel($"Payment failed: {message.FailureReason}");
            
            _reservationRepository.Update(reservation);
            await _reservationRepository.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Successfully cancelled Reservation {ReservationId} due to payment failure", 
                message.ReservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderPaymentFailed for Reservation {ReservationId}", 
                message.ReservationId);
            throw;
        }
    }
}

/// <summary>
/// Consumer for order cancelled events from Ticketing Service
/// </summary>
public class OrderCancelledConsumer : IConsumer<OrderCancelledIntegrationEvent>
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<OrderCancelledConsumer> _logger;

    public OrderCancelledConsumer(
        IReservationRepository reservationRepository,
        IEventRepository eventRepository,
        ILogger<OrderCancelledConsumer> logger)
    {
        _reservationRepository = reservationRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderCancelledIntegrationEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Processing OrderCancelled for Order {OrderId}, Event {EventId}", 
            message.OrderId, message.EventId);

        try
        {
            // If there's a reservation, cancel it
            if (message.ReservationId.HasValue)
            {
                var reservation = await _reservationRepository.GetByIdAsync(message.ReservationId.Value, context.CancellationToken);
                if (reservation != null)
                {
                    reservation.Cancel(message.CancellationReason);
                    _reservationRepository.Update(reservation);
                }
            }

            // Restock tickets if needed
            if (message.TicketsToRestock.Any())
            {
                var eventAggregate = await _eventRepository.GetByIdAsync(message.EventId, context.CancellationToken);
                if (eventAggregate != null)
                {
                    // This would typically involve restocking specific ticket types
                    // Implementation depends on how tickets are tracked
                    _logger.LogInformation("Restocking {Count} tickets for Event {EventId}", 
                        message.TicketsToRestock.Count, message.EventId);
                }
            }

            await _reservationRepository.SaveChangesAsync(context.CancellationToken);

            _logger.LogInformation("Successfully processed OrderCancelled for Order {OrderId}", message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process OrderCancelled for Order {OrderId}", message.OrderId);
            throw;
        }
    }
}
