using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Text.Json;

namespace Event.API.Middleware;

/// <summary>
/// Global exception handler middleware
/// </summary>
public class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing the request");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var problemDetails = exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(validationEx),
            InvalidOperationException invalidOpEx => CreateInvalidOperationProblemDetails(invalidOpEx),
            ArgumentException argEx => CreateArgumentProblemDetails(argEx),
            UnauthorizedAccessException => CreateUnauthorizedProblemDetails(),
            NotImplementedException => CreateNotImplementedProblemDetails(),
            _ => CreateInternalServerErrorProblemDetails(exception)
        };

        // Set the response status code
        context.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;

        // Add correlation ID if available
        if (context.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId))
        {
            problemDetails.Extensions["correlationId"] = correlationId.ToString();
        }

        // Add request ID
        problemDetails.Extensions["requestId"] = context.TraceIdentifier;

        // Add timestamp
        problemDetails.Extensions["timestamp"] = DateTime.UtcNow;

        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        };

        var json = JsonSerializer.Serialize(problemDetails, jsonOptions);
        await context.Response.WriteAsync(json);
    }

    private ProblemDetails CreateValidationProblemDetails(ValidationException validationException)
    {
        var problemDetails = new ValidationProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = "One or more validation errors occurred.",
            Instance = null
        };

        foreach (var error in validationException.Errors)
        {
            if (problemDetails.Errors.ContainsKey(error.PropertyName))
            {
                problemDetails.Errors[error.PropertyName] = problemDetails.Errors[error.PropertyName]
                    .Concat(new[] { error.ErrorMessage }).ToArray();
            }
            else
            {
                problemDetails.Errors[error.PropertyName] = new[] { error.ErrorMessage };
            }
        }

        return problemDetails;
    }

    private ProblemDetails CreateInvalidOperationProblemDetails(InvalidOperationException exception)
    {
        var statusCode = DetermineStatusCodeFromMessage(exception.Message);

        return new ProblemDetails
        {
            Type = GetProblemTypeUrl(statusCode),
            Title = GetTitleForStatusCode(statusCode),
            Status = (int)statusCode,
            Detail = exception.Message,
            Instance = null
        };
    }

    private ProblemDetails CreateArgumentProblemDetails(ArgumentException exception)
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Bad Request",
            Status = (int)HttpStatusCode.BadRequest,
            Detail = exception.Message,
            Instance = null
        };
    }

    private ProblemDetails CreateUnauthorizedProblemDetails()
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1",
            Title = "Unauthorized",
            Status = (int)HttpStatusCode.Unauthorized,
            Detail = "Authentication is required to access this resource.",
            Instance = null
        };
    }

    private ProblemDetails CreateNotImplementedProblemDetails()
    {
        return new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.2",
            Title = "Not Implemented",
            Status = (int)HttpStatusCode.NotImplemented,
            Detail = "This functionality is not yet implemented.",
            Instance = null
        };
    }

    private ProblemDetails CreateInternalServerErrorProblemDetails(Exception exception)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Title = "Internal Server Error",
            Status = (int)HttpStatusCode.InternalServerError,
            Detail = _environment.IsDevelopment() 
                ? exception.ToString() 
                : "An error occurred while processing your request.",
            Instance = null
        };

        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = new
            {
                type = exception.GetType().Name,
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message
            };
        }

        return problemDetails;
    }

    private static HttpStatusCode DetermineStatusCodeFromMessage(string message)
    {
        // Determine status code based on common error message patterns
        var lowerMessage = message.ToLowerInvariant();

        if (lowerMessage.Contains("not found"))
            return HttpStatusCode.NotFound;

        if (lowerMessage.Contains("already exists") || lowerMessage.Contains("duplicate") || lowerMessage.Contains("conflict"))
            return HttpStatusCode.Conflict;

        if (lowerMessage.Contains("unauthorized") || lowerMessage.Contains("access denied"))
            return HttpStatusCode.Unauthorized;

        if (lowerMessage.Contains("forbidden"))
            return HttpStatusCode.Forbidden;

        if (lowerMessage.Contains("concurrency") || lowerMessage.Contains("version"))
            return HttpStatusCode.Conflict;

        return HttpStatusCode.BadRequest;
    }

    private static string GetProblemTypeUrl(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            HttpStatusCode.Unauthorized => "https://tools.ietf.org/html/rfc7235#section-3.1",
            HttpStatusCode.Forbidden => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
            HttpStatusCode.NotFound => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            HttpStatusCode.Conflict => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
            HttpStatusCode.InternalServerError => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            _ => "https://tools.ietf.org/html/rfc7231"
        };
    }

    private static string GetTitleForStatusCode(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.Unauthorized => "Unauthorized",
            HttpStatusCode.Forbidden => "Forbidden",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.Conflict => "Conflict",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            _ => "Error"
        };
    }
}

/// <summary>
/// Extension method to register the global exception handler middleware
/// </summary>
public static class GlobalExceptionHandlerMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlerMiddleware>();
    }
}
