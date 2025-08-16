using Event.Application.Common.Models;
using MediatR;

namespace Event.Application.Features.Events.Queries.GetEvent;

/// <summary>
/// Query to get a single event by ID
/// </summary>
public record GetEventQuery : IRequest<EventDto?>
{
    public Guid EventId { get; init; }
    public bool IncludeTicketTypes { get; init; } = true;
    public bool IncludePricingRules { get; init; } = true;
    public bool IncludeAllocations { get; init; } = true;

    public GetEventQuery(Guid eventId)
    {
        EventId = eventId;
    }

    public GetEventQuery(Guid eventId, bool includeTicketTypes, bool includePricingRules, bool includeAllocations)
    {
        EventId = eventId;
        IncludeTicketTypes = includeTicketTypes;
        IncludePricingRules = includePricingRules;
        IncludeAllocations = includeAllocations;
    }
}

/// <summary>
/// Query to get a single event by slug
/// </summary>
public record GetEventBySlugQuery : IRequest<EventDto?>
{
    public string Slug { get; init; }
    public Guid OrganizationId { get; init; }
    public bool IncludeTicketTypes { get; init; } = true;
    public bool IncludePricingRules { get; init; } = true;
    public bool IncludeAllocations { get; init; } = true;

    public GetEventBySlugQuery(string slug, Guid organizationId)
    {
        Slug = slug;
        OrganizationId = organizationId;
    }

    public GetEventBySlugQuery(string slug, Guid organizationId, bool includeTicketTypes, bool includePricingRules, bool includeAllocations)
    {
        Slug = slug;
        OrganizationId = organizationId;
        IncludeTicketTypes = includeTicketTypes;
        IncludePricingRules = includePricingRules;
        IncludeAllocations = includeAllocations;
    }
}
