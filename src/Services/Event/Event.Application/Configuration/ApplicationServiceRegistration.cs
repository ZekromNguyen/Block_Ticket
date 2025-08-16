using Event.Application.Common.Interfaces;
using Event.Application.EventHandlers;
using Event.Application.Validators;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shared.Common.Models;
using System.Reflection;

namespace Event.Application.Configuration;

/// <summary>
/// Application layer service registration
/// </summary>
public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR
        AddMediatR(services);

        // Add FluentValidation
        AddValidation(services);

        // Add Application Services
        AddApplicationServices(services);

        // Add Domain Event Handlers
        AddDomainEventHandlers(services);

        // Add AutoMapper (if using)
        // AddAutoMapper(services);

        return services;
    }

    private static void AddMediatR(IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            
            // Add pipeline behaviors
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });
    }

    private static void AddValidation(IServiceCollection services)
    {
        // Register all validators from the assembly
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Register specific validators (commented out until request classes are created)
        // services.AddScoped<IValidator<CreateEventRequest>, CreateEventRequestValidator>();
        // services.AddScoped<IValidator<UpdateEventRequest>, UpdateEventRequestValidator>();
        // services.AddScoped<IValidator<SearchEventsRequest>, SearchEventsRequestValidator>();
        // services.AddScoped<IValidator<GetEventsRequest>, GetEventsRequestValidator>();

        // services.AddScoped<IValidator<CreateVenueRequest>, CreateVenueRequestValidator>();
        // services.AddScoped<IValidator<UpdateVenueRequest>, UpdateVenueRequestValidator>();
        // services.AddScoped<IValidator<SearchVenuesRequest>, SearchVenuesRequestValidator>();
        // services.AddScoped<IValidator<ImportSeatMapRequest>, ImportSeatMapRequestValidator>();

        // services.AddScoped<IValidator<CreateReservationRequest>, CreateReservationRequestValidator>();
        // services.AddScoped<IValidator<CreateTicketTypeRequest>, CreateTicketTypeRequestValidator>();
        // services.AddScoped<IValidator<CreatePricingRuleRequest>, CreatePricingRuleRequestValidator>();
    }

    private static void AddApplicationServices(IServiceCollection services)
    {
        // Register application services when implemented
        // services.AddScoped<IEventService, EventService>();
        // services.AddScoped<IVenueService, VenueService>();
        // services.AddScoped<IReservationService, ReservationService>();
        // services.AddScoped<ITicketTypeService, TicketTypeService>();
        // services.AddScoped<IPricingService, PricingService>();
        // services.AddScoped<IAllocationService, AllocationService>();
        // services.AddScoped<IEventSeriesService, EventSeriesService>();
        // services.AddScoped<IAvailabilityService, AvailabilityService>();
        // services.AddScoped<ICatalogService, CatalogService>();
        // services.AddScoped<IReportingService, ReportingService>();

        // Register utility services
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();
    }

    private static void AddDomainEventHandlers(IServiceCollection services)
    {
        // Domain event handlers are automatically registered by MediatR
        // But we can explicitly register them if needed
        services.AddScoped<EventCreatedDomainEventHandler>();
        services.AddScoped<EventPublishedDomainEventHandler>();
        services.AddScoped<EventCancelledDomainEventHandler>();
        services.AddScoped<InventoryChangedDomainEventHandler>();
        services.AddScoped<ReservationCreatedDomainEventHandler>();
        services.AddScoped<ReservationConfirmedDomainEventHandler>();
        services.AddScoped<ReservationExpiredDomainEventHandler>();
        services.AddScoped<ReservationCancelledDomainEventHandler>();
    }
}

/// <summary>
/// MediatR pipeline behavior for validation
/// </summary>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Any())
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}

/// <summary>
/// MediatR pipeline behavior for logging
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var userId = _currentUserService.UserId;
        var correlationId = _currentUserService.CorrelationId;

        _logger.LogInformation("Event Service Request: {Name} {@UserId} {@CorrelationId} {@Request}",
            requestName, userId, correlationId, request);

        var response = await next();

        _logger.LogInformation("Event Service Response: {Name} {@UserId} {@CorrelationId}",
            requestName, userId, correlationId);

        return response;
    }
}

/// <summary>
/// MediatR pipeline behavior for performance monitoring
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private readonly ICurrentUserService _currentUserService;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger, ICurrentUserService currentUserService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var response = await next();

        stopwatch.Stop();

        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500) // Log if request takes longer than 500ms
        {
            var requestName = typeof(TRequest).Name;
            var userId = _currentUserService.UserId;
            var correlationId = _currentUserService.CorrelationId;

            _logger.LogWarning("Event Service Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@UserId} {@CorrelationId} {@Request}",
                requestName, elapsedMilliseconds, userId, correlationId, request);
        }

        return response;
    }
}

/// <summary>
/// Domain event publisher implementation
/// </summary>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IMediator _mediator;

    public DomainEventPublisher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task PublishAsync<TDomainEvent>(TDomainEvent domainEvent, CancellationToken cancellationToken = default)
        where TDomainEvent : IDomainEvent
    {
        await _mediator.Publish(domainEvent, cancellationToken);
    }

    public async Task PublishAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }
}

/// <summary>
/// Date time provider implementation
/// </summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Now => DateTime.Now;
    public DateTimeOffset UtcNowOffset => DateTimeOffset.UtcNow;
    public DateTimeOffset NowOffset => DateTimeOffset.Now;
}

// CurrentUserService implementation moved to Infrastructure layer

// NotificationService implementation moved to Infrastructure layer

// IntegrationEventPublisher implementation moved to Infrastructure layer
