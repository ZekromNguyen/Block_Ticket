using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.EventSeries.Queries.GetEventSeries;

public record GetEventSeriesBySlugQuery(
    string Slug,
    Guid OrganizationId,
    bool IncludeEvents = false
    ) : IRequest<EventSeriesDto?>;
