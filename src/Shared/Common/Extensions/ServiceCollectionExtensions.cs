using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Serilog;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

namespace Shared.Common.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers shared infrastructure (Serilog logging, OpenTelemetry tracing + metrics)
    /// used by every microservice. Reads:
    ///  - Service:Name (used as the OpenTelemetry service name and Serilog property)
    ///  - OpenTelemetry:Otlp:Endpoint (optional; when set, an OTLP exporter is added)
    /// </summary>
    public static IServiceCollection AddSharedServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add Serilog
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        var serviceName = configuration["Service:Name"] ?? "blockticket";
        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var otlpEndpoint = configuration["OpenTelemetry:Otlp:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: serviceName, serviceVersion: typeof(ServiceCollectionExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0")
                .AddAttributes(new KeyValuePair<string, object>[]
                {
                    new("deployment.environment", environmentName),
                }))
            .WithTracing(builder =>
            {
                builder
                    .AddSource("MassTransit")
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                {
                    builder.AddOtlpExporter(opt => opt.Endpoint = new System.Uri(otlpEndpoint));
                }
            })
            .WithMetrics(builder =>
            {
                builder
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }

    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        var serviceName = builder.Configuration["Service:Name"] ?? "blockticket";
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Service", serviceName)
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {Service} {CorrelationId} {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}