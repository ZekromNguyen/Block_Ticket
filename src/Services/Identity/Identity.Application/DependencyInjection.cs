using FluentValidation;
using Identity.Application.Services;
using Identity.Domain.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Identity.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Add FluentValidation
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        // Add AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        // Add Application Services
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IMfaApplicationService, MfaApplicationService>();
        services.AddScoped<IOAuthService, OAuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IGatewayService, GatewayService>();

        // Add Domain Services
        services.AddScoped<UserDomainService>();

        // Add Validation Behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}

// Validation Pipeline Behavior
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

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .Where(r => r.Errors.Any())
                .SelectMany(r => r.Errors)
                .ToList();

            if (failures.Any())
            {
                var errorMessages = failures.Select(f => f.ErrorMessage).ToList();
                
                // If TResponse is Result or Result<T>, return failure result
                if (typeof(TResponse).IsGenericType && typeof(TResponse).GetGenericTypeDefinition() == typeof(Identity.Application.Common.Models.Result<>))
                {
                    var resultType = typeof(TResponse).GetGenericArguments()[0];
                    var failureMethod = typeof(Identity.Application.Common.Models.Result<>)
                        .MakeGenericType(resultType)
                        .GetMethod("Failure", new[] { typeof(List<string>) });
                    
                    return (TResponse)failureMethod!.Invoke(null, new object[] { errorMessages })!;
                }
                else if (typeof(TResponse) == typeof(Identity.Application.Common.Models.Result))
                {
                    return (TResponse)(object)Identity.Application.Common.Models.Result.Failure(errorMessages);
                }

                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
