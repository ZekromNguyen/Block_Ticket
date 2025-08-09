using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Net;

namespace Identity.API.Middleware;

/// <summary>
/// Global exception filter to handle unhandled exceptions
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly ILogger<GlobalExceptionFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public void OnException(ExceptionContext context)
    {
        var exception = context.Exception;
        var request = context.HttpContext.Request;

        _logger.LogError(exception, 
            "Unhandled exception occurred. Request: {Method} {Path} {QueryString}",
            request.Method, request.Path, request.QueryString);

        var problemDetails = CreateProblemDetails(exception, context.HttpContext);
        
        context.Result = new ObjectResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };

        context.ExceptionHandled = true;
    }

    private ProblemDetails CreateProblemDetails(Exception exception, HttpContext httpContext)
    {
        var statusCode = GetStatusCode(exception);
        var title = GetTitle(exception);
        var detail = GetDetail(exception);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Add additional details in development
        if (_environment.IsDevelopment())
        {
            problemDetails.Extensions["exception"] = exception.GetType().Name;
            problemDetails.Extensions["stackTrace"] = exception.StackTrace;
            
            if (exception.InnerException != null)
            {
                problemDetails.Extensions["innerException"] = exception.InnerException.Message;
            }
        }

        // Add trace ID for correlation
        if (httpContext.TraceIdentifier != null)
        {
            problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        }

        return problemDetails;
    }

    private static int GetStatusCode(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => (int)HttpStatusCode.BadRequest,
            ArgumentException => (int)HttpStatusCode.BadRequest,
            InvalidOperationException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            NotImplementedException => (int)HttpStatusCode.NotImplemented,
            TimeoutException => (int)HttpStatusCode.RequestTimeout,
            _ => (int)HttpStatusCode.InternalServerError
        };
    }

    private static string GetTitle(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => "Bad Request",
            ArgumentException => "Bad Request",
            InvalidOperationException => "Bad Request",
            UnauthorizedAccessException => "Unauthorized",
            NotImplementedException => "Not Implemented",
            TimeoutException => "Request Timeout",
            _ => "Internal Server Error"
        };
    }

    private string GetDetail(Exception exception)
    {
        if (_environment.IsDevelopment())
        {
            return exception.Message;
        }

        return exception switch
        {
            ArgumentNullException => "The request contains null arguments.",
            ArgumentException => "The request contains invalid arguments.",
            InvalidOperationException => "The requested operation is not valid.",
            UnauthorizedAccessException => "Access to the requested resource is unauthorized.",
            NotImplementedException => "The requested feature is not implemented.",
            TimeoutException => "The request timed out.",
            _ => "An error occurred while processing the request."
        };
    }
}
