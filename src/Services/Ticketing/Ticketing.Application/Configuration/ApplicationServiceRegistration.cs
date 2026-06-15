using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Ticketing.Application.Configuration;

public static class ApplicationServiceRegistration
{
    public static IServiceCollection AddTicketingApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        return services;
    }
}
