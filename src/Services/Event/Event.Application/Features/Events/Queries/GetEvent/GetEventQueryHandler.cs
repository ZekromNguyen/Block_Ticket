using Event.Application.Common.Interfaces;
using Event.Application.Common.Models;
using Event.Domain.Entities;
using Event.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Event.Application.Features.Events.Queries.GetEvent;

/// <summary>
/// Handler for GetEventQuery
/// </summary>
public class GetEventQueryHandler : IRequestHandler<GetEventQuery, EventDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventQueryHandler> _logger;

    public GetEventQueryHandler(
        IEventRepository eventRepository,
        ICacheService cacheService,
        ILogger<GetEventQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<EventDto?> Handle(GetEventQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting event {EventId}", request.EventId);

        // Try to get from cache first
        var cacheKey = GetCacheKey(request.EventId, request.IncludeTicketTypes, request.IncludePricingRules, request.IncludeAllocations);
        var cachedEvent = await _cacheService.GetAsync<EventDto>(cacheKey, cancellationToken);
        
        if (cachedEvent != null)
        {
            _logger.LogDebug("Event {EventId} found in cache", request.EventId);
            return cachedEvent;
        }

        // Get from database
        var eventAggregate = await GetEventWithIncludes(request, cancellationToken);
        
        if (eventAggregate == null)
        {
            _logger.LogInformation("Event {EventId} not found", request.EventId);
            return null;
        }

        // Convert to DTO
        var eventDto = MapToDto(eventAggregate, request);

        // Cache the result
        await _cacheService.SetAsync(cacheKey, eventDto, TimeSpan.FromMinutes(5), cancellationToken);

        _logger.LogInformation("Successfully retrieved event {EventId}", request.EventId);
        return eventDto;
    }

    private async Task<EventAggregate?> GetEventWithIncludes(GetEventQuery request, CancellationToken cancellationToken)
    {
        if (request.IncludeTicketTypes || request.IncludePricingRules || request.IncludeAllocations)
        {
            // Get with full details
            return await _eventRepository.GetWithFullDetailsAsync(request.EventId, cancellationToken);
        }
        else
        {
            // Get basic event only
            return await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        }
    }

    internal static EventDto MapToDto(EventAggregate eventAggregate, GetEventQuery request)
    {
        var dto = new EventDto
        {
            Id = eventAggregate.Id,
            Title = eventAggregate.Title,
            Description = eventAggregate.Description,
            Slug = eventAggregate.Slug.Value,
            OrganizationId = eventAggregate.OrganizationId,
            PromoterId = eventAggregate.PromoterId,
            VenueId = eventAggregate.VenueId,
            Status = eventAggregate.Status.ToString(),
            EventDate = eventAggregate.EventDate,
            TimeZone = eventAggregate.TimeZone.Value,
            PublishStartDate = eventAggregate.PublishWindow?.StartDate,
            PublishEndDate = eventAggregate.PublishWindow?.EndDate,
            ImageUrl = eventAggregate.ImageUrl,
            BannerUrl = eventAggregate.BannerUrl,
            SeoTitle = eventAggregate.SeoTitle,
            SeoDescription = eventAggregate.SeoDescription,
            Categories = eventAggregate.Categories.ToList(),
            Tags = eventAggregate.Tags.ToList(),
            Version = eventAggregate.Version,
            CreatedAt = eventAggregate.CreatedAt,
            UpdatedAt = eventAggregate.UpdatedAt
        };

        // Map related entities based on request
        if (request.IncludeTicketTypes)
        {
            dto.TicketTypes = eventAggregate.TicketTypes.Select(MapTicketTypeToDto).ToList();
        }

        if (request.IncludePricingRules)
        {
            dto.PricingRules = eventAggregate.PricingRules.Select(MapPricingRuleToDto).ToList();
        }

        if (request.IncludeAllocations)
        {
            dto.Allocations = eventAggregate.Allocations.Select(MapAllocationToDto).ToList();
        }

        return dto;
    }

    private static TicketTypeDto MapTicketTypeToDto(TicketType ticketType)
    {
        return new TicketTypeDto
        {
            Id = ticketType.Id,
            EventId = ticketType.EventId,
            Name = ticketType.Name,
            Code = ticketType.Code,
            Description = ticketType.Description,
            InventoryType = ticketType.InventoryType.ToString(),
            BasePrice = new MoneyDto { Amount = ticketType.BasePrice.Amount, Currency = ticketType.BasePrice.Currency },
            ServiceFee = ticketType.ServiceFee != null ? new MoneyDto { Amount = ticketType.ServiceFee.Amount, Currency = ticketType.ServiceFee.Currency } : null,
            TaxAmount = ticketType.TaxAmount != null ? new MoneyDto { Amount = ticketType.TaxAmount.Amount, Currency = ticketType.TaxAmount.Currency } : null,
            Capacity = new CapacityDto { Total = ticketType.Capacity.Total, Available = ticketType.Capacity.Available },
            MinPurchaseQuantity = ticketType.MinPurchaseQuantity,
            MaxPurchaseQuantity = ticketType.MaxPurchaseQuantity,
            MaxPerCustomer = ticketType.MaxPerCustomer,
            IsVisible = ticketType.IsVisible,
            IsResaleAllowed = ticketType.IsResaleAllowed,
            RequiresApproval = ticketType.RequiresApproval,
            OnSaleWindows = ticketType.OnSaleWindows.Select(w => new OnSaleWindowDto
            {
                StartDate = w.StartDate,
                EndDate = w.EndDate,
                TimeZone = w.TimeZone.Value
            }).ToList(),
            CreatedAt = ticketType.CreatedAt,
            UpdatedAt = ticketType.UpdatedAt
        };
    }

    private static PricingRuleDto MapPricingRuleToDto(PricingRule pricingRule)
    {
        return new PricingRuleDto
        {
            Id = pricingRule.Id,
            EventId = pricingRule.EventId,
            Name = pricingRule.Name,
            Description = pricingRule.Description,
            Type = pricingRule.Type.ToString(),
            Priority = pricingRule.Priority,
            IsActive = pricingRule.IsActive,
            EffectiveFrom = pricingRule.EffectiveFrom,
            EffectiveTo = pricingRule.EffectiveTo,
            DiscountType = pricingRule.DiscountType?.ToString(),
            DiscountValue = pricingRule.DiscountValue,
            MaxDiscountAmount = pricingRule.MaxDiscountAmount != null ? new MoneyDto { Amount = pricingRule.MaxDiscountAmount.Amount, Currency = pricingRule.MaxDiscountAmount.Currency } : null,
            MinOrderAmount = pricingRule.MinOrderAmount != null ? new MoneyDto { Amount = pricingRule.MinOrderAmount.Amount, Currency = pricingRule.MinOrderAmount.Currency } : null,
            MinQuantity = pricingRule.MinQuantity,
            MaxQuantity = pricingRule.MaxQuantity,
            DiscountCode = pricingRule.DiscountCode,
            IsSingleUse = pricingRule.IsSingleUse,
            MaxUses = pricingRule.MaxUses,
            CurrentUses = pricingRule.CurrentUses,
            TargetTicketTypeIds = pricingRule.TargetTicketTypeIds?.ToList(),
            TargetCustomerSegments = pricingRule.TargetCustomerSegments?.ToList(),
            CreatedAt = pricingRule.CreatedAt,
            UpdatedAt = pricingRule.UpdatedAt
        };
    }

    private static AllocationDto MapAllocationToDto(Allocation allocation)
    {
        return new AllocationDto
        {
            Id = allocation.Id,
            EventId = allocation.EventId,
            TicketTypeId = allocation.TicketTypeId,
            Name = allocation.Name,
            Description = allocation.Description,
            Type = allocation.Type.ToString(),
            Quantity = allocation.Quantity,
            AllocatedQuantity = allocation.AllocatedQuantity,
            AccessCode = allocation.AccessCode,
            AvailableFrom = allocation.AvailableFrom,
            AvailableUntil = allocation.AvailableUntil,
            ExpiresAt = allocation.ExpiresAt,
            IsActive = allocation.IsActive,
            IsExpired = allocation.IsExpired,
            AllowedUserIds = allocation.AllowedUserIds?.ToList(),
            AllowedEmailDomains = allocation.AllowedEmailDomains?.ToList(),
            AllocatedSeatIds = allocation.AllocatedSeatIds.ToList(),
            CreatedAt = allocation.CreatedAt,
            UpdatedAt = allocation.UpdatedAt
        };
    }

    private static string GetCacheKey(Guid eventId, bool includeTicketTypes, bool includePricingRules, bool includeAllocations)
    {
        var includes = new List<string>();
        if (includeTicketTypes) includes.Add("tickets");
        if (includePricingRules) includes.Add("pricing");
        if (includeAllocations) includes.Add("allocations");
        
        var includesStr = includes.Any() ? $":{string.Join(",", includes)}" : "";
        return $"event:{eventId}{includesStr}";
    }
}

/// <summary>
/// Handler for GetEventBySlugQuery
/// </summary>
public class GetEventBySlugQueryHandler : IRequestHandler<GetEventBySlugQuery, EventDto?>
{
    private readonly IEventRepository _eventRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetEventBySlugQueryHandler> _logger;

    public GetEventBySlugQueryHandler(
        IEventRepository eventRepository,
        ICacheService cacheService,
        ILogger<GetEventBySlugQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<EventDto?> Handle(GetEventBySlugQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting event by slug {Slug} for organization {OrganizationId}", 
            request.Slug, request.OrganizationId);

        // Try to get from cache first
        var cacheKey = GetCacheKey(request.Slug, request.OrganizationId, request.IncludeTicketTypes, request.IncludePricingRules, request.IncludeAllocations);
        var cachedEvent = await _cacheService.GetAsync<EventDto>(cacheKey, cancellationToken);
        
        if (cachedEvent != null)
        {
            _logger.LogDebug("Event with slug {Slug} found in cache", request.Slug);
            return cachedEvent;
        }

        // Get from database
        var eventAggregate = await _eventRepository.GetBySlugAsync(request.Slug, request.OrganizationId, cancellationToken);
        
        if (eventAggregate == null)
        {
            _logger.LogInformation("Event with slug {Slug} not found for organization {OrganizationId}", 
                request.Slug, request.OrganizationId);
            return null;
        }

        // Convert to DTO using the same mapping logic
        var getEventQuery = new GetEventQuery(eventAggregate.Id, request.IncludeTicketTypes, request.IncludePricingRules, request.IncludeAllocations);
        var eventDto = GetEventQueryHandler.MapToDto(eventAggregate, getEventQuery);

        // Cache the result
        await _cacheService.SetAsync(cacheKey, eventDto, TimeSpan.FromMinutes(5), cancellationToken);

        _logger.LogInformation("Successfully retrieved event {EventId} by slug {Slug}", eventAggregate.Id, request.Slug);
        return eventDto;
    }

    private static string GetCacheKey(string slug, Guid organizationId, bool includeTicketTypes, bool includePricingRules, bool includeAllocations)
    {
        var includes = new List<string>();
        if (includeTicketTypes) includes.Add("tickets");
        if (includePricingRules) includes.Add("pricing");
        if (includeAllocations) includes.Add("allocations");
        
        var includesStr = includes.Any() ? $":{string.Join(",", includes)}" : "";
        return $"event:slug:{organizationId}:{slug}{includesStr}";
    }
}
