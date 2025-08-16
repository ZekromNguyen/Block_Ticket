# ETag Optimistic Concurrency Implementation

## Overview

The Event Service now includes comprehensive ETag-based optimistic concurrency control for inventory management. This prevents race conditions when multiple users attempt to reserve tickets simultaneously, ensuring data consistency and preventing overselling.

## Features Implemented

✅ **ETag Value Object**: Strongly-typed ETag with validation and comparison capabilities  
✅ **ETaggable Base Entity**: Automatic ETag generation and validation for domain entities  
✅ **ETag Middleware**: HTTP ETag header processing for conditional requests  
✅ **Repository Support**: ETag-aware repository operations with atomic updates  
✅ **Inventory Protection**: Atomic ticket reservation with ETag validation  
✅ **Conditional HTTP Operations**: Support for If-Match, If-None-Match headers  
✅ **Database Configuration**: EF Core configuration for ETag storage and indexing  

## How It Works

### 1. ETag Generation

Each entity automatically generates an ETag based on:
- Entity ID and last modified timestamp
- Critical properties (inventory, status, version)
- Entity-specific data (capacity, reservations, pricing)

```csharp
// EventAggregate ETag includes:
// - Title, Status, Version, EventDate
// - Total/Available capacity
// - Ticket type inventory snapshot
// - Pricing and allocation counts

// TicketType ETag includes:
// - Name, Code, Price, Inventory type
// - Capacity (Total, Reserved, Available)
// - Purchase constraints and visibility
```

### 2. HTTP Operations

#### **GET Requests with ETags**
```http
GET /api/v1/events/123
→ Response includes: ETag: "abc123def456"

GET /api/v1/events/123
If-None-Match: "abc123def456"
→ 304 Not Modified (if unchanged)
```

#### **Update Requests with Validation**
```http
PUT /api/v1/reservations
If-Match: "abc123def456"
→ 200 OK (if ETag matches)
→ 412 Precondition Failed (if ETag mismatch)
```

### 3. Atomic Inventory Operations

```csharp
// Atomic ticket reservation with ETag validation
var success = await _eventRepository.TryReserveTicketsWithETagAsync(
    eventId: eventId,
    ticketTypeId: ticketTypeId, 
    quantity: 2,
    expectedETag: clientETag,
    cancellationToken: cancellationToken
);

if (!success)
{
    // ETag mismatch or insufficient inventory
    // Return current state to client
}
```

## Usage Examples

### 1. Ticket Reservation Controller

```csharp
[RequireETag]
[HttpPost]
public async Task<ActionResult> CreateReservation([FromBody] CreateReservationRequest request)
{
    // Get ETag from If-Match header
    var expectedETag = HttpContext.GetIfMatchETagObject("EventAggregate", request.EventId.ToString());
    
    // Atomic reservation with ETag validation
    var success = await _eventRepository.TryReserveTicketsWithETagAsync(
        request.EventId, request.TicketTypeId, request.Quantity, expectedETag);
    
    if (!success)
    {
        // Handle ETag mismatch - return current inventory state
        return StatusCode(412, new { 
            error = "Inventory Changed",
            currentETag = await GetCurrentETag(request.EventId)
        });
    }
    
    // Set new ETag in response
    Response.SetETag(updatedETag);
    return Ok(reservation);
}
```

### 2. Inventory Status Endpoint

```csharp
[HttpGet("events/{eventId}/inventory")]
public async Task<ActionResult<InventoryStatusResponse>> GetEventInventoryStatus(Guid eventId)
{
    var (available, sold, etag) = await _eventRepository.GetInventorySummaryWithETagAsync(eventId);
    
    // Set ETag for conditional requests
    Response.SetETag(etag);
    
    // Check If-None-Match for 304 responses
    if (Request.Headers.IfNoneMatch.Contains(etag.ToHttpHeaderValue()))
    {
        return StatusCode(304); // Not Modified
    }
    
    return Ok(new InventoryStatusResponse
    {
        EventId = eventId,
        Available = available,
        Sold = sold,
        ETag = etag.Value
    });
}
```

### 3. Client Usage

```javascript
// JavaScript client example
class TicketClient {
    async reserveTickets(eventId, ticketTypeId, quantity) {
        // 1. Get current event state with ETag
        const inventoryResponse = await fetch(`/api/v1/reservations/events/${eventId}/inventory`);
        const inventory = await inventoryResponse.json();
        const currentETag = inventoryResponse.headers.get('ETag');
        
        // 2. Check availability
        if (inventory.available < quantity) {
            throw new Error('Insufficient tickets available');
        }
        
        // 3. Attempt reservation with ETag
        const reservationResponse = await fetch('/api/v1/reservations', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'If-Match': currentETag  // Critical for concurrency control
            },
            body: JSON.stringify({
                eventId, ticketTypeId, quantity, customerEmail: 'user@example.com'
            })
        });
        
        if (reservationResponse.status === 412) {
            // Precondition Failed - inventory changed
            const error = await reservationResponse.json();
            throw new Error(`Inventory changed: ${error.message}`);
        }
        
        return await reservationResponse.json();
    }
}
```

## Database Schema

### ETag Storage
```sql
-- Added to all ETaggable entities
ALTER TABLE events ADD COLUMN etag_value VARCHAR(64) NOT NULL;
ALTER TABLE events ADD COLUMN etag_updated_at TIMESTAMP NOT NULL;
ALTER TABLE events ADD COLUMN row_version BYTEA; -- Additional concurrency token

-- Indexes for performance
CREATE INDEX IX_events_etag ON events(etag_value);
CREATE INDEX IX_events_etag_timestamp ON events(etag_updated_at);

-- Constraints
ALTER TABLE events ADD CONSTRAINT CK_events_etag_not_empty 
    CHECK (LENGTH(etag_value) > 0);
```

### Optimistic Concurrency
```sql
-- ETag as concurrency token
UPDATE events 
SET title = 'New Title', etag_value = 'new_etag_value', etag_updated_at = NOW()
WHERE id = $1 AND etag_value = $2; -- Will fail if ETag doesn't match

-- Row version for additional protection
UPDATE events 
SET title = 'New Title', row_version = row_version + 1
WHERE id = $1 AND row_version = $2;
```

## Race Condition Prevention

### Before ETag Implementation
```
User A: GET /events/123 → {available: 5}
User B: GET /events/123 → {available: 5}
User A: POST /reservations {quantity: 3} → Success (available: 2)
User B: POST /reservations {quantity: 4} → Success! (oversold by 2)
```

### After ETag Implementation
```
User A: GET /events/123 → {available: 5, ETag: "v1"}
User B: GET /events/123 → {available: 5, ETag: "v1"}
User A: POST /reservations If-Match: "v1" {quantity: 3} → Success (available: 2, ETag: "v2")
User B: POST /reservations If-Match: "v1" {quantity: 4} → 412 Precondition Failed
User B: GET /events/123 → {available: 2, ETag: "v2"} (refresh required)
```

## Error Handling

### ETag Mismatch Response
```json
{
  "error": "Precondition Failed",
  "message": "The event inventory has been modified since your last request",
  "expectedETag": "v1",
  "currentETag": "v2",
  "currentInventory": {
    "available": 2,
    "sold": 18,
    "etag": "v2"
  }
}
```

### Missing ETag Response
```json
{
  "error": "Precondition Required", 
  "message": "If-Match header with event ETag is required for this operation"
}
```

## Performance Impact

### Benefits
- **Prevents Race Conditions**: Eliminates overselling scenarios
- **Atomic Operations**: Single database transaction for validation + update
- **Client Optimization**: 304 Not Modified reduces bandwidth
- **Database Efficiency**: Index-based ETag lookups

### Overhead
- **Storage**: ~70 bytes per entity (ETag value + timestamp + row version)
- **Network**: ~20 bytes per response (ETag header)
- **CPU**: Minimal (hash generation + comparison)

## Middleware Configuration

### Attributes for Controllers

```csharp
[RequireETag]          // Requires If-Match header for updates
[SupportsETag]         // Enables ETag generation and validation
[NoETag]              // Explicitly disables ETag processing
```

### Automatic Processing

The ETag middleware automatically:
1. **GET/HEAD**: Generates ETags and handles If-None-Match
2. **PUT/PATCH/DELETE**: Validates If-Match headers
3. **Error Responses**: Returns 412 Precondition Failed for mismatches

## Testing

### Unit Tests
```csharp
[Test]
public async Task ReserveTickets_WithValidETag_Should_Succeed()
{
    // Arrange
    var eventEntity = CreateTestEvent();
    var originalETag = eventEntity.CurrentETag;
    
    // Act
    var success = await _repository.TryReserveTicketsWithETagAsync(
        eventEntity.Id, ticketTypeId, 2, originalETag);
    
    // Assert
    Assert.IsTrue(success);
    Assert.AreNotEqual(originalETag, eventEntity.CurrentETag); // ETag updated
}

[Test]
public async Task ReserveTickets_WithStaleETag_Should_Fail()
{
    // Arrange
    var eventEntity = CreateTestEvent();
    var staleETag = ETag.FromTimestamp("Event", eventEntity.Id.ToString(), DateTime.UtcNow.AddMinutes(-1));
    
    // Act
    var success = await _repository.TryReserveTicketsWithETagAsync(
        eventEntity.Id, ticketTypeId, 2, staleETag);
    
    // Assert
    Assert.IsFalse(success); // Should fail with stale ETag
}
```

### Integration Tests
```csharp
[Test]
public async Task ConcurrentReservations_Should_PreventOverselling()
{
    // Arrange
    var eventId = await CreateEventWithCapacity(5);
    var client1 = CreateTestClient();
    var client2 = CreateTestClient();
    
    // Act - Concurrent reservations
    var task1 = client1.ReserveTicketsAsync(eventId, 3);
    var task2 = client2.ReserveTicketsAsync(eventId, 4);
    
    var results = await Task.WhenAll(task1, task2);
    
    // Assert - Only one should succeed
    var successCount = results.Count(r => r.IsSuccess);
    Assert.AreEqual(1, successCount, "Only one reservation should succeed");
    
    var finalInventory = await GetEventInventory(eventId);
    Assert.IsTrue(finalInventory.Available >= 0, "No overselling occurred");
}
```

## Production Considerations

### Monitoring
- Track ETag mismatch rates (high rates indicate contention)
- Monitor 412 Precondition Failed responses
- Alert on ETag validation failures

### Scaling
- ETags work across multiple application instances
- Database-level concurrency control ensures consistency
- Consider read replicas for ETag generation

### Client Implementation
- Implement ETag caching in client applications
- Handle 412 errors gracefully with retry logic
- Use conditional requests to reduce bandwidth

This ETag implementation provides robust protection against inventory race conditions while maintaining high performance and scalability.
