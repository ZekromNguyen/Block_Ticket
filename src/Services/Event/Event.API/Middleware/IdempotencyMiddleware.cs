using Event.Application.Interfaces.Infrastructure;
using Microsoft.Extensions.Primitives;
using System.Text;

namespace Event.API.Middleware;

/// <summary>
/// Middleware for handling idempotency keys automatically
/// </summary>
public class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;
    private readonly HashSet<string> _idempotentMethods = new(StringComparer.OrdinalIgnoreCase) 
    { 
        "POST", "PUT", "PATCH" 
    };

    public IdempotencyMiddleware(RequestDelegate next, ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IIdempotencyService idempotencyService)
    {
        // Only apply idempotency to mutating operations
        if (!_idempotentMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Check for idempotency key header
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out StringValues idempotencyKeyValues) ||
            string.IsNullOrWhiteSpace(idempotencyKeyValues.FirstOrDefault()))
        {
            // Idempotency key is required for mutating operations
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Idempotency-Key header is required for this operation");
            return;
        }

        var idempotencyKey = idempotencyKeyValues.FirstOrDefault()!;

        // Validate idempotency key format
        if (!idempotencyService.IsValidIdempotencyKey(idempotencyKey))
        {
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid Idempotency-Key format");
            return;
        }

        try
        {
            // Read request body
            var requestBody = await ReadRequestBodyAsync(context.Request);
            var requestHeaders = SerializeHeaders(context.Request.Headers);
            
            // Extract user and organization context
            var userId = context.User?.FindFirst("sub")?.Value;
            var orgIdClaim = context.User?.FindFirst("org_id")?.Value;
            Guid? organizationId = Guid.TryParse(orgIdClaim, out var orgId) ? orgId : null;
            var requestId = context.TraceIdentifier;

            // Check for duplicate request
            var duplicateCheck = await idempotencyService.CheckDuplicateAsync(
                idempotencyKey,
                context.Request.Path,
                context.Request.Method,
                requestBody,
                context.RequestAborted);

            if (duplicateCheck.IsDuplicate && duplicateCheck.ExistingRecord != null)
            {
                if (duplicateCheck.IsProcessing)
                {
                    // Request is currently being processed
                    context.Response.StatusCode = 409;
                    await context.Response.WriteAsync("Request is already being processed");
                    return;
                }

                if (duplicateCheck.ConflictReason != null)
                {
                    // Request parameters don't match
                    context.Response.StatusCode = 422;
                    await context.Response.WriteAsync($"Idempotency key conflict: {duplicateCheck.ConflictReason}");
                    return;
                }

                if (duplicateCheck.ExistingRecord.IsSuccessful())
                {
                    // Return cached response
                    context.Response.StatusCode = duplicateCheck.ExistingRecord.ResponseStatusCode;
                    
                    if (!string.IsNullOrEmpty(duplicateCheck.ExistingRecord.ResponseHeaders))
                    {
                        // Restore response headers (if needed)
                    }

                    if (!string.IsNullOrEmpty(duplicateCheck.ExistingRecord.ResponseBody))
                    {
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(duplicateCheck.ExistingRecord.ResponseBody);
                    }

                    _logger.LogDebug("Returned cached response for idempotency key: {IdempotencyKey}", idempotencyKey);
                    return;
                }
            }

            // Reset request body stream for downstream middleware
            if (!string.IsNullOrEmpty(requestBody))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                context.Request.Body = new MemoryStream(bodyBytes);
            }

            // Store idempotency context for downstream use
            context.Items["IdempotencyKey"] = idempotencyKey;
            context.Items["IdempotencyRequestBody"] = requestBody;
            context.Items["IdempotencyRequestHeaders"] = requestHeaders;
            context.Items["IdempotencyUserId"] = userId;
            context.Items["IdempotencyOrganizationId"] = organizationId;
            context.Items["IdempotencyRequestId"] = requestId;

            // Create response capture stream
            var originalBodyStream = context.Response.Body;
            using var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            try
            {
                await _next(context);

                // Capture response
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                
                // Store successful response
                if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
                {
                    await idempotencyService.CompleteRequestAsync(
                        idempotencyKey,
                        new { response = responseBody },
                        context.Response.StatusCode,
                        null,
                        context.RequestAborted);
                }

                // Copy response back to original stream
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                await responseBodyStream.CopyToAsync(originalBodyStream);
            }
            catch (Exception ex)
            {
                // Store error response
                var errorResponse = new { error = ex.Message, type = ex.GetType().Name };
                await idempotencyService.CompleteRequestAsync(
                    idempotencyKey,
                    errorResponse,
                    500,
                    null,
                    context.RequestAborted);

                throw;
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing idempotent request for key: {IdempotencyKey}", idempotencyKey);
            
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Internal server error");
            }
        }
    }

    private static async Task<string?> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.Body == null || !request.Body.CanRead)
            return null;

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(request.Body, encoding: Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        
        request.Body.Position = 0;
        return body;
    }

    private static string SerializeHeaders(IHeaderDictionary headers)
    {
        var relevantHeaders = headers
            .Where(h => !h.Key.StartsWith("Idempotency", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(h => h.Key, h => h.Value.ToString());

        return System.Text.Json.JsonSerializer.Serialize(relevantHeaders);
    }
}
