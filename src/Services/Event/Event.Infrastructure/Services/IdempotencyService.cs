using Event.Domain.Entities;
using Event.Domain.Interfaces;
using Event.Application.Interfaces.Infrastructure;
using Event.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Event.Infrastructure.Services;

/// <summary>
/// Service implementation for handling idempotency logic
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly IIdempotencyRepository _idempotencyRepository;
    private readonly ILogger<IdempotencyService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public IdempotencyService(
        IIdempotencyRepository idempotencyRepository,
        ILogger<IdempotencyService> logger)
    {
        _idempotencyRepository = idempotencyRepository;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<IdempotencyResult<TResponse>> ProcessRequestAsync<TResponse>(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        string? requestHeaders,
        Func<CancellationToken, Task<TResponse>> requestHandler,
        string? userId = null,
        Guid? organizationId = null,
        string? requestId = null,
        TimeSpan? ttl = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidIdempotencyKey(idempotencyKey))
        {
            throw new ArgumentException($"Invalid idempotency key format: {idempotencyKey}");
        }

        // Check for duplicate request
        var duplicateCheck = await CheckDuplicateAsync(idempotencyKey, requestPath, httpMethod, requestBody, cancellationToken);
        
        if (duplicateCheck.IsDuplicate && duplicateCheck.ExistingRecord != null)
        {
            // Return existing response
            if (duplicateCheck.ExistingRecord.IsSuccessful())
            {
                var existingResponse = JsonSerializer.Deserialize<TResponse>(
                    duplicateCheck.ExistingRecord.ResponseBody ?? "null", _jsonOptions);

                _logger.LogDebug("Returning cached response for idempotency key: {IdempotencyKey}", idempotencyKey);

                return new IdempotencyResult<TResponse>
                {
                    Response = existingResponse!,
                    IsNewRequest = false,
                    StatusCode = duplicateCheck.ExistingRecord.ResponseStatusCode,
                    ResponseHeaders = duplicateCheck.ExistingRecord.ResponseHeaders,
                    ProcessedAt = duplicateCheck.ExistingRecord.ProcessedAt
                };
            }
            else if (duplicateCheck.IsProcessing)
            {
                throw new InvalidOperationException("Request is already being processed");
            }
        }

        // Create or get idempotency record
        var (record, isNew) = await _idempotencyRepository.GetOrCreateAsync(
            idempotencyKey,
            requestPath,
            httpMethod,
            requestBody,
            requestHeaders,
            userId,
            organizationId,
            requestId,
            ttl,
            cancellationToken);

        if (!isNew && record.IsSuccessful())
        {
            // Another thread completed the request
            var cachedResponse = JsonSerializer.Deserialize<TResponse>(
                record.ResponseBody ?? "null", _jsonOptions);

            return new IdempotencyResult<TResponse>
            {
                Response = cachedResponse!,
                IsNewRequest = false,
                StatusCode = record.ResponseStatusCode,
                ResponseHeaders = record.ResponseHeaders,
                ProcessedAt = record.ProcessedAt
            };
        }

        try
        {
            // Execute the request
            _logger.LogDebug("Processing new request for idempotency key: {IdempotencyKey}", idempotencyKey);
            var response = await requestHandler(cancellationToken);

            // Store the successful response
            var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
            await _idempotencyRepository.UpdateResponseAsync(
                idempotencyKey,
                responseJson,
                200,
                null,
                cancellationToken);

            return new IdempotencyResult<TResponse>
            {
                Response = response,
                IsNewRequest = true,
                StatusCode = 200,
                ResponseHeaders = null,
                ProcessedAt = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            // Store the error response
            _logger.LogError(ex, "Error processing request for idempotency key: {IdempotencyKey}", idempotencyKey);
            
            var errorResponse = new { error = ex.Message, type = ex.GetType().Name };
            var errorJson = JsonSerializer.Serialize(errorResponse, _jsonOptions);
            
            await _idempotencyRepository.UpdateResponseAsync(
                idempotencyKey,
                errorJson,
                500,
                null,
                cancellationToken);

            throw;
        }
    }

    public async Task<IdempotencyCheckResult> CheckDuplicateAsync(
        string idempotencyKey,
        string requestPath,
        string httpMethod,
        string? requestBody,
        CancellationToken cancellationToken = default)
    {
        var existingRecord = await _idempotencyRepository.GetByKeyAsync(idempotencyKey, cancellationToken);

        if (existingRecord == null)
        {
            return new IdempotencyCheckResult
            {
                IsDuplicate = false,
                IsProcessing = false
            };
        }

        if (existingRecord.IsExpired())
        {
            _logger.LogDebug("Idempotency record expired for key: {IdempotencyKey}", idempotencyKey);
            return new IdempotencyCheckResult
            {
                IsDuplicate = false,
                IsProcessing = false
            };
        }

        // Check if request parameters match
        bool parametersMatch = 
            existingRecord.RequestPath.Equals(requestPath, StringComparison.OrdinalIgnoreCase) &&
            existingRecord.HttpMethod.Equals(httpMethod, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existingRecord.RequestBody, requestBody, StringComparison.Ordinal);

        if (!parametersMatch)
        {
            return new IdempotencyCheckResult
            {
                IsDuplicate = true,
                IsProcessing = false,
                ExistingRecord = existingRecord,
                ConflictReason = "Request parameters do not match existing record"
            };
        }

        return new IdempotencyCheckResult
        {
            IsDuplicate = true,
            IsProcessing = existingRecord.IsProcessing(),
            ExistingRecord = existingRecord
        };
    }

    public async Task CompleteRequestAsync(
        string idempotencyKey,
        object response,
        int statusCode,
        string? responseHeaders,
        CancellationToken cancellationToken = default)
    {
        var responseJson = JsonSerializer.Serialize(response, _jsonOptions);
        await _idempotencyRepository.UpdateResponseAsync(
            idempotencyKey,
            responseJson,
            statusCode,
            responseHeaders,
            cancellationToken);
    }

    public async Task<TResponse?> GetStoredResponseAsync<TResponse>(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await _idempotencyRepository.GetByKeyAsync(idempotencyKey, cancellationToken);
        
        if (record?.ResponseBody == null || record.IsExpired())
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<TResponse>(record.ResponseBody, _jsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize stored response for key: {IdempotencyKey}", idempotencyKey);
            return default;
        }
    }

    public bool IsValidIdempotencyKey(string? idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            return false;

        try
        {
            _ = new IdempotencyKey(idempotencyKey);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> CleanupExpiredRecordsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting cleanup of expired idempotency records");
        var removedCount = await _idempotencyRepository.RemoveExpiredAsync(cancellationToken);
        _logger.LogInformation("Cleaned up {Count} expired idempotency records", removedCount);
        return removedCount;
    }
}
