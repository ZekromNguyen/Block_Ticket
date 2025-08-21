using Event.Domain.ValueObjects;
using Event.Application.Interfaces.Application;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Net;
using System.Text;
using System.Text.Json;
using ETagMismatchException = Event.Domain.ValueObjects.ETagMismatchException;

namespace Event.API.Middleware;

/// <summary>
/// Middleware to handle ETag processing for HTTP requests and responses
/// </summary>
public class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var actionDescriptor = endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>();

        // Check if the endpoint supports ETags
        var requiresETag = actionDescriptor?.MethodInfo.GetCustomAttributes(typeof(RequireETagAttribute), true).Any() == true ||
                          actionDescriptor?.ControllerTypeInfo.GetCustomAttributes(typeof(RequireETagAttribute), true).Any() == true;

        var supportsETag = actionDescriptor?.MethodInfo.GetCustomAttributes(typeof(SupportsETagAttribute), true).Any() == true ||
                          actionDescriptor?.ControllerTypeInfo.GetCustomAttributes(typeof(SupportsETagAttribute), true).Any() == true ||
                          requiresETag;

        if (!supportsETag)
        {
            await _next(context);
            return;
        }

        // Handle conditional requests for GET/HEAD
        if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
        {
            await HandleConditionalGetAsync(context);
            return;
        }

        // Handle conditional updates for PUT/PATCH/DELETE
        if (HttpMethods.IsPut(context.Request.Method) || 
            HttpMethods.IsPatch(context.Request.Method) || 
            HttpMethods.IsDelete(context.Request.Method))
        {
            await HandleConditionalUpdateAsync(context, requiresETag);
            return;
        }

        // For other methods, just pass through
        await _next(context);
    }

    private async Task HandleConditionalGetAsync(HttpContext context)
    {
        // Capture the original response stream
        var originalBodyStream = context.Response.Body;
        using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);

            // Only process successful responses
            if (context.Response.StatusCode == (int)HttpStatusCode.OK)
            {
                // Generate ETag from response body
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseContent = await new StreamReader(responseBodyStream).ReadToEndAsync();
                
                if (!string.IsNullOrEmpty(responseContent))
                {
                    var etag = GenerateETagFromContent(responseContent);
                    context.Response.Headers.ETag = etag.ToHttpHeaderValue();

                    // Check If-None-Match header
                    var ifNoneMatch = context.Request.Headers.IfNoneMatch.FirstOrDefault();
                    if (!string.IsNullOrEmpty(ifNoneMatch))
                    {
                        if (ETag.TryParse(ifNoneMatch, "Response", "Content", out var clientETag) && 
                            etag.Matches(clientETag))
                        {
                            // Return 304 Not Modified
                            context.Response.StatusCode = (int)HttpStatusCode.NotModified;
                            context.Response.ContentLength = 0;
                            
                            _logger.LogDebug("Returned 304 Not Modified for matching ETag: {ETag}", etag.Value);
                            return;
                        }
                    }
                }

                // Copy the response back to the original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            else
            {
                // For non-200 responses, just copy the content
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private async Task HandleConditionalUpdateAsync(HttpContext context, bool requiresETag)
    {
        try
        {
            // Check for If-Match header
            var ifMatch = context.Request.Headers.IfMatch.FirstOrDefault();
            
            if (requiresETag && string.IsNullOrEmpty(ifMatch))
            {
                context.Response.StatusCode = (int)HttpStatusCode.PreconditionRequired;
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Precondition Required",
                    message = "If-Match header with ETag is required for this operation",
                    timestamp = DateTime.UtcNow
                });
                return;
            }

            if (!string.IsNullOrEmpty(ifMatch))
            {
                // Store the ETag for use in controllers
                context.Items["IfMatch"] = ifMatch;
                _logger.LogDebug("Stored If-Match ETag: {ETag}", ifMatch);
            }

            await _next(context);

            // Handle 412 Precondition Failed responses
            if (context.Response.StatusCode == (int)HttpStatusCode.PreconditionFailed)
            {
                _logger.LogWarning("ETag mismatch resulted in 412 Precondition Failed for {Method} {Path}", 
                    context.Request.Method, context.Request.Path);
            }
        }
        catch (ETagMismatchException ex)
        {
            _logger.LogWarning(ex, "ETag mismatch in middleware: {Message}", ex.Message);
            
            context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Precondition Failed",
                message = ex.Message,
                expectedETag = ex.ExpectedETag,
                providedETag = ex.ActualETag,
                timestamp = DateTime.UtcNow
            });
        }
        catch (ETagRequiredException ex)
        {
            _logger.LogWarning(ex, "ETag required but not provided: {Message}", ex.Message);
            
            context.Response.StatusCode = (int)HttpStatusCode.PreconditionRequired;
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Precondition Required",
                message = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private static ETag GenerateETagFromContent(string content)
    {
        // Generate a simple ETag from content hash
        return ETag.FromTimestamp("Response", "Content", DateTime.UtcNow, content);
    }
}

/// <summary>
/// Attribute to mark actions/controllers that require ETag for conditional updates
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireETagAttribute : Attribute
{
    /// <summary>
    /// Whether to require ETag for GET requests as well
    /// </summary>
    public bool RequireForGet { get; set; } = false;

    /// <summary>
    /// Custom error message when ETag is missing
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Attribute to mark actions/controllers that support ETag for conditional requests
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class SupportsETagAttribute : Attribute
{
    /// <summary>
    /// Whether to generate ETags for GET responses
    /// </summary>
    public bool GenerateForGet { get; set; } = true;

    /// <summary>
    /// Whether to support conditional updates
    /// </summary>
    public bool SupportConditionalUpdates { get; set; } = true;
}

/// <summary>
/// Extension methods for ETag handling in controllers
/// </summary>
public static class ETagExtensions
{
    /// <summary>
    /// Gets the If-Match ETag from the request
    /// </summary>
    public static string? GetIfMatchETag(this HttpContext context)
    {
        return context.Items.TryGetValue("IfMatch", out var etag) ? etag as string : null;
    }

    /// <summary>
    /// Gets the If-Match ETag as an ETag object
    /// </summary>
    public static ETag? GetIfMatchETagObject(this HttpContext context, string entityType, string entityId)
    {
        var etagValue = GetIfMatchETag(context);
        if (string.IsNullOrEmpty(etagValue))
            return null;

        if (ETag.TryParse(etagValue, entityType, entityId, out var etag))
            return etag;

        return null;
    }

    /// <summary>
    /// Sets the ETag header in the response
    /// </summary>
    public static void SetETag(this HttpResponse response, ETag etag)
    {
        response.Headers.ETag = etag.ToHttpHeaderValue();
    }

    /// <summary>
    /// Sets the ETag header from a string value
    /// </summary>
    public static void SetETag(this HttpResponse response, string etagValue)
    {
        response.Headers.ETag = $"\"{etagValue}\"";
    }

    /// <summary>
    /// Validates the If-Match ETag against an entity's current ETag
    /// </summary>
    public static void ValidateIfMatchETag<T>(this HttpContext context, T entity) 
        where T : class, IETaggable
    {
        var providedETag = GetIfMatchETag(context);
        entity.ValidateETag(providedETag);
    }

    /// <summary>
    /// Returns a 304 Not Modified response
    /// </summary>
    public static async Task Return304NotModifiedAsync(this HttpContext context, ETag etag)
    {
        context.Response.StatusCode = (int)HttpStatusCode.NotModified;
        context.Response.Headers.ETag = etag.ToHttpHeaderValue();
        context.Response.ContentLength = 0;
        
        await context.Response.CompleteAsync();
    }

    /// <summary>
    /// Returns a 412 Precondition Failed response
    /// </summary>
    public static async Task Return412PreconditionFailedAsync(this HttpContext context, string message, string? expectedETag = null, string? providedETag = null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.PreconditionFailed;
        
        await context.Response.WriteAsJsonAsync(new
        {
            error = "Precondition Failed",
            message,
            expectedETag,
            providedETag,
            timestamp = DateTime.UtcNow
        });
    }
}
