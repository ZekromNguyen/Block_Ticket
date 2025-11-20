using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.ValueObjects;
using Event.Domain.Interfaces;

using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.TicketTypes.Commands.CreateTicketType;

public class CreateTicketTypeCommandHandler : IRequestHandler<CreateTicketTypeCommand, TicketTypeDto>
{
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<CreateTicketTypeCommandHandler> _logger;

    public CreateTicketTypeCommandHandler(IEventRepository eventRepository, ILogger<CreateTicketTypeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<TicketTypeDto> Handle(CreateTicketTypeCommand request, CancellationToken cancellationToken)
    {
                var eventAggregate = await _eventRepository.GetWithFullDetailsAsync(request.EventId, cancellationToken);
        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event with ID '{request.EventId}' not found");
        }

        if (eventAggregate == null)
        {
            throw new InvalidOperationException($"Event with ID '{request.EventId}' not found");
        }

        var ticketType = new TicketType(
            request.EventId,
            request.Name,
            request.Code,
            new Money(request.BasePrice.Amount, request.BasePrice.Currency),
            request.InventoryType,
            100, // Placeholder capacity
            request.Description
        );

        eventAggregate.AddTicketType(ticketType);

        _eventRepository.Update(eventAggregate);

        await _eventRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created ticket type {TicketTypeName} for event {EventId}", request.Name, request.EventId);

        return TicketTypeDto.FromEntity(ticketType);
    }
}

