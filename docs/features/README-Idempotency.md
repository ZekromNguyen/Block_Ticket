# Idempotency Implementation Guide

## Overview

This Event Service implements comprehensive idempotency support to ensure that critical operations can be safely retried without causing unintended side effects. Idempotency is crucial for:

- **Network reliability**: Handling network timeouts and retries
- **Distributed systems**: Ensuring consistency across service boundaries  
- **User experience**: Preventing duplicate charges, bookings, or reservations
- **Enterprise requirements**: Meeting SLA requirements for critical operations

## Features

✅ **Automatic Middleware**: Transparent idempotency for all POST/PUT/PATCH operations  
✅ **Manual Control**: Fine-grained control with service injection  
✅ **Configurable TTL**: Custom time-to-live for different operation types  
✅ **Race Condition Safe**: Atomic database operations prevent duplicates  
✅ **Background Cleanup**: Automatic cleanup of expired records  
✅ **Rich Metadata**: Request/response tracking with audit trail  
✅ **Conflict Detection**: Validates request parameters match  

## Quick Start

### 1. Basic Usage (Automatic)

Add the `Idempotency-Key` header to your POST/PUT/PATCH requests:

```bash
curl -X POST "https://api.example.com/api/v1/events" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: idem_12345678-1234-1234-1234-123456789abc" \
  -d '{"title": "My Event", "description": "Event description"}'
```

### 2. Controller Decoration

```csharp
[HttpPost]
[Idempotent(TTL = 24)] // 24 hours TTL
public async Task<ActionResult<EventDto>> CreateEvent([FromBody] CreateEventRequest request)
{
    // Your business logic here
    var eventDto = await _eventService.CreateEventAsync(request);
    return Ok(eventDto);
}
```

### 3. Manual Service Usage

```csharp
public async Task<ReservationDto> CreateReservationWithIdempotency(
    string idempotencyKey,
    CreateReservationRequest request)
{
    var result = await _idempotencyService.ProcessRequestAsync<ReservationDto>(
        idempotencyKey,
        "/api/v1/reservations",
        "POST",
        JsonSerializer.Serialize(request),
        null,
        async (ct) =>
        {
            // Your business logic here
            return await _reservationService.CreateAsync(request, ct);
        });

    return result.Response;
}
```

## Configuration

### Middleware Setup (Program.cs)

```csharp
// Add idempotency middleware (order matters!)
app.UseMiddleware<IdempotencyMiddleware>();
```

### Service Registration

```csharp
// Services are auto-registered via InfrastructureServiceRegistration
services.AddScoped<IIdempotencyService, IdempotencyService>();
services.AddScoped<IIdempotencyRepository, IdempotencyRepository>();
services.AddHostedService<IdempotencyCleanupService>();
```

## Attributes

### `[Idempotent]`

Marks an action as requiring idempotency.

```csharp
[Idempotent(TTL = 24, Required = true)]
public async Task<ActionResult> MyAction() { ... }
```

**Parameters:**
- `TTL`: Time-to-live in hours (default: 24)
- `Required`: Whether idempotency key is required (default: true)
- `AutoGenerate`: Auto-generate key if missing (default: false)
- `MissingKeyMessage`: Custom error message

### `[NoIdempotency]`

Excludes an action from automatic idempotency checks.

```csharp
[NoIdempotency]
public async Task<ActionResult> ReadOnlyAction() { ... }
```

## Idempotency Key Format

Keys must follow this pattern: `^[a-zA-Z0-9\-_]{1,255}$`

**Valid Examples:**
- `idem_12345678-1234-1234-1234-123456789abc`
- `user-123_action-456`
- `reservation-retry-001`

**Invalid Examples:**
- `key with spaces` (contains spaces)
- `key@domain.com` (contains special characters)
- `""` (empty string)

## HTTP Response Codes

| Code | Scenario |
|------|----------|
| 200  | ✅ Success (new or cached response) |
| 400  | ❌ Missing or invalid Idempotency-Key |
| 409  | ⚠️ Request currently being processed |
| 422  | ❌ Idempotency key conflict (different parameters) |
| 500  | ❌ Server error during processing |

## Best Practices

### 1. Key Generation Strategy

```csharp
// Client-side UUID generation
var idempotencyKey = $"reservation_{Guid.NewGuid():N}";

// Operation-specific keys
var idempotencyKey = $"payment_{orderId}_{userId}_{timestamp}";

// Retry-safe keys (same operation = same key)
var idempotencyKey = $"{operationType}_{resourceId}_{clientRequestId}";
```

### 2. TTL Selection

| Operation Type | Recommended TTL | Reason |
|----------------|-----------------|---------|
| Payments | 24-48 hours | Financial reconciliation |
| Reservations | 1-6 hours | Short-lived business logic |
| User Registration | 24 hours | Email verification flows |
| Data Import | 7 days | Large batch operations |

### 3. Error Handling

```csharp
try 
{
    var response = await httpClient.PostAsync(url, content, headers);
    
    if (response.StatusCode == HttpStatusCode.Conflict)
    {
        // Request is being processed, wait and retry
        await Task.Delay(TimeSpan.FromSeconds(5));
        return await RetryRequest();
    }
    
    if (response.StatusCode == (HttpStatusCode)422)
    {
        // Key conflict - different parameters
        throw new InvalidOperationException("Request parameters changed");
    }
    
    return await response.Content.ReadAsStringAsync();
}
catch (HttpRequestException ex)
{
    // Network error - safe to retry with same key
    _logger.LogWarning(ex, "Network error, retrying with same idempotency key");
    return await RetryRequest();
}
```

## Database Schema

The idempotency system uses a dedicated table:

```sql
CREATE TABLE idempotency_records (
    id UUID PRIMARY KEY,
    idempotency_key VARCHAR(255) UNIQUE NOT NULL,
    request_path VARCHAR(2048) NOT NULL,
    http_method VARCHAR(10) NOT NULL,
    request_body TEXT,
    request_headers JSONB,
    response_body TEXT,
    response_status_code INTEGER DEFAULT 0,
    response_headers JSONB,
    processed_at TIMESTAMP WITH TIME ZONE NOT NULL,
    expires_at TIMESTAMP WITH TIME ZONE NOT NULL,
    user_id VARCHAR(100),
    organization_id UUID,
    request_id VARCHAR(100) NOT NULL
);

-- Indexes for performance
CREATE UNIQUE INDEX idx_idempotency_records_key ON idempotency_records(idempotency_key);
CREATE INDEX idx_idempotency_records_expires_at ON idempotency_records(expires_at);
CREATE INDEX idx_idempotency_records_user_id ON idempotency_records(user_id);
CREATE INDEX idx_idempotency_records_organization_id ON idempotency_records(organization_id);
```

## Monitoring and Observability

### Key Metrics

- **Idempotency Hit Rate**: `cached_responses / total_requests`
- **Cleanup Frequency**: Records cleaned per cleanup cycle
- **Conflict Rate**: 422 responses / total_idempotent_requests
- **Processing Conflicts**: 409 responses (concurrent processing)

### Log Examples

```json
{
  "timestamp": "2024-01-15T10:30:00Z",
  "level": "INFO",
  "message": "Returning cached response for idempotency key",
  "idempotencyKey": "idem_abc123",
  "originalProcessedAt": "2024-01-15T10:25:00Z",
  "statusCode": 200
}

{
  "timestamp": "2024-01-15T10:30:00Z", 
  "level": "WARN",
  "message": "Idempotency key conflict detected",
  "idempotencyKey": "idem_abc123",
  "conflictReason": "Request parameters do not match existing record"
}
```

## Troubleshooting

### Common Issues

#### 1. "Invalid Idempotency-Key format"
**Cause**: Key contains invalid characters or exceeds length limit  
**Solution**: Use only alphanumeric, hyphens, and underscores (max 255 chars)

#### 2. "Request is already being processed" (409)
**Cause**: Concurrent requests with same idempotency key  
**Solution**: Implement exponential backoff retry logic

#### 3. "Idempotency key conflict" (422)
**Cause**: Same key used with different request parameters  
**Solution**: Generate new key or verify request parameters

#### 4. Memory/Storage Growth
**Cause**: Cleanup service not running or misconfigured  
**Solution**: Check `IdempotencyCleanupService` is registered and running

### Debugging Commands

```bash
# Check recent idempotency records
curl "https://api.example.com/api/v1/idempotentreservations/idempotency/{key}"

# Trigger manual cleanup
curl -X POST "https://api.example.com/api/v1/idempotentreservations/idempotency/cleanup"

# Database queries
SELECT COUNT(*) FROM idempotency_records WHERE expires_at > NOW();
SELECT * FROM idempotency_records WHERE idempotency_key = 'your-key';
```

## Performance Considerations

### Database Optimization

- **Partitioning**: Consider partitioning by date for high-volume scenarios
- **Indexes**: Monitor query performance and add indexes as needed
- **Cleanup**: Run cleanup during low-traffic periods
- **Connection Pooling**: Ensure proper connection pooling configuration

### Memory Usage

- **Request Body Storage**: Large request bodies are stored as text
- **Response Caching**: Successful responses are cached in full
- **TTL Management**: Shorter TTLs reduce storage requirements

### Scaling

- **Horizontal**: Service is stateless and scales horizontally
- **Database**: Consider read replicas for idempotency checks
- **Cleanup**: Distribute cleanup across multiple instances if needed

## Integration Examples

### With Payment Services

```csharp
[HttpPost("payments")]
[Idempotent(TTL = 48)] // Longer TTL for financial operations
public async Task<ActionResult<PaymentDto>> ProcessPayment(
    [FromBody] PaymentRequest request)
{
    // Idempotency ensures payment is only processed once
    var payment = await _paymentService.ProcessAsync(request);
    return Ok(payment);
}
```

### With Event Sourcing

```csharp
public async Task<EventDto> CreateEventWithIdempotency(
    string idempotencyKey,
    CreateEventCommand command)
{
    return await _idempotencyService.ProcessRequestAsync<EventDto>(
        idempotencyKey,
        $"/events/{command.EventId}",
        "POST",
        JsonSerializer.Serialize(command),
        null,
        async (ct) =>
        {
            // Event sourcing - idempotency prevents duplicate events
            await _eventStore.AppendAsync(command.EventId, command, ct);
            return await _eventProjectionService.GetEventAsync(command.EventId, ct);
        });
}
```

## Testing

### Unit Tests

```csharp
[Test]
public async Task ProcessRequestAsync_WithSameKey_ReturnsCachedResponse()
{
    // Arrange
    var key = "test-key";
    var request = new CreateEventRequest { Title = "Test Event" };
    
    // Act - First call
    var result1 = await _idempotencyService.ProcessRequestAsync<EventDto>(
        key, "/events", "POST", JsonSerializer.Serialize(request), null,
        async ct => new EventDto { Id = Guid.NewGuid(), Title = request.Title });
    
    // Act - Second call with same key
    var result2 = await _idempotencyService.ProcessRequestAsync<EventDto>(
        key, "/events", "POST", JsonSerializer.Serialize(request), null,
        async ct => new EventDto { Id = Guid.NewGuid(), Title = request.Title });
    
    // Assert
    Assert.That(result1.IsNewRequest, Is.True);
    Assert.That(result2.IsNewRequest, Is.False);
    Assert.That(result1.Response.Id, Is.EqualTo(result2.Response.Id));
}
```

### Integration Tests

```csharp
[Test]
public async Task CreateEvent_WithIdempotencyKey_PreventsDuplicates()
{
    // Arrange
    var client = _factory.CreateClient();
    var request = new CreateEventRequest { Title = "Test Event" };
    var idempotencyKey = $"test_{Guid.NewGuid():N}";
    
    // Act - Send same request twice
    var response1 = await client.PostAsync("/api/v1/events", 
        JsonContent.Create(request), 
        new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey });
        
    var response2 = await client.PostAsync("/api/v1/events",
        JsonContent.Create(request),
        new Dictionary<string, string> { ["Idempotency-Key"] = idempotencyKey });
    
    // Assert
    Assert.That(response1.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    Assert.That(response2.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    
    var event1 = await response1.Content.ReadFromJsonAsync<EventDto>();
    var event2 = await response2.Content.ReadFromJsonAsync<EventDto>();
    
    Assert.That(event1.Id, Is.EqualTo(event2.Id)); // Same event returned
}
```

---

## Support

For issues or questions about the idempotency implementation:

1. Check the logs for detailed error messages
2. Verify idempotency key format and headers
3. Monitor database performance and cleanup
4. Review the troubleshooting section above

For additional help, please refer to the main Event Service documentation or create an issue in the project repository.
