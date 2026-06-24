using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Shared.Common.Extensions;

/// <summary>
/// Reads or generates an X-Correlation-Id for each HTTP request, exposes it via
/// <see cref="HttpContext.Items"/>, pushes it onto the current OpenTelemetry activity as a tag,
/// enriches the Serilog log scope, and echoes it on the response so downstream services can
/// propagate the same value.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
                            !string.IsNullOrWhiteSpace(incoming.ToString())
            ? incoming.ToString()
            : Guid.NewGuid().ToString("N");

        context.Items[HttpContextItemKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var activity = System.Diagnostics.Activity.Current;
        activity?.SetTag("correlation_id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }

    public static string? GetCorrelationId(this HttpContext context)
    {
        return context.Items.TryGetValue(CorrelationIdMiddleware.HttpContextItemKey, out var value)
            ? value as string
            : null;
    }
}