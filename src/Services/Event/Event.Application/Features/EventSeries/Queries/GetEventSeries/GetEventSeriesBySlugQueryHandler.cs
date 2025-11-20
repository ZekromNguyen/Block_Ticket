using Event.Domain.Interfaces;
using Event.Application.Common.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.EventSeries.Queries.GetEventSeries;

public class GetEventSeriesBySlugQueryHandler : IRequestHandler<GetEventSeriesBySlugQuery, EventSeriesDto?>
{
    private readonly IEventSeriesRepository _eventSeriesRepository;
    private readonly ILogger<GetEventSeriesBySlugQueryHandler> _logger;

    public GetEventSeriesBySlugQueryHandler(IEventSeriesRepository eventSeriesRepository, ILogger<GetEventSeriesBySlugQueryHandler> logger)
    {
        _eventSeriesRepository = eventSeriesRepository;
        _logger = logger;
    }

    public async Task<EventSeriesDto?> Handle(GetEventSeriesBySlugQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting event series by slug {Slug} for organization {OrganizationId}", request.Slug, request.OrganizationId);

        var eventSeries = await _eventSeriesRepository.GetBySlugAsync(request.Slug, request.OrganizationId, request.IncludeEvents, cancellationToken);

        if (eventSeries == null)
        {
            _logger.LogWarning("Event series with slug {Slug} not found for organization {OrganizationId}", request.Slug, request.OrganizationId);
            return null;
        }

        return EventSeriesDto.FromEntity(eventSeries);
    }
}
