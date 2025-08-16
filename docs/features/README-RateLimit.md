# Rate Limiting Implementation Guide

## Overview

The Event Service implements comprehensive multi-layered rate limiting to protect against abuse, ensure fair resource usage, and maintain API performance. This enterprise-grade rate limiting system provides:

- **Multi-layer Protection**: IP, Client, Organization, and Endpoint-specific limits
- **Distributed Support**: Redis-backed for horizontal scaling
- **Flexible Configuration**: Per-endpoint customization with multiple time windows
- **Progressive Enforcement**: Escalating restrictions for repeated violations
- **Real-time Monitoring**: Comprehensive metrics and admin controls

## Features

✅ **Multi-Layer Rate Limiting**: IP + Client + Organization + Endpoint specific limits  
✅ **Advanced Middleware**: Automatic protection for all API endpoints  
✅ **Attribute-Based Control**: Fine-grained per-action customization  
✅ **Redis Distribution**: Scalable across multiple instances  
✅ **Progressive Penalties**: Escalating restrictions for violators  
✅ **Whitelist Support**: Bypass protection for trusted clients  
✅ **Admin Interface**: Real-time monitoring and management  
✅ **Comprehensive Metrics**: Detailed analytics and reporting  
✅ **Configurable Responses**: Custom error messages and status codes  

## Architecture

### Rate Limiting Layers (in order of enforcement)

1. **IP-Based Limits** - Most restrictive, prevents basic abuse
2. **Client-Based Limits** - Per authenticated client/API key
3. **Organization-Based Limits** - Per tenant/organization quotas
4. **Endpoint-Specific Limits** - Custom rules per API endpoint

### Request Flow

```
Incoming Request
    ↓
IP Whitelist Check → Allow if whitelisted
    ↓
Client Whitelist Check → Allow if whitelisted
    ↓
IP Rate Limit Check → Block if exceeded
    ↓
Client Rate Limit Check → Block if exceeded
    ↓
Organization Rate Limit Check → Block if exceeded
    ↓
Endpoint-Specific Rate Limit Check → Block if exceeded
    ↓
Record Request Metrics
    ↓
Process Request
```

## Configuration

### Default Rate Limits

| Layer | Time Window | Limit | Purpose |
|-------|-------------|-------|---------|
| **IP** | 1 second | 10 requests | Burst protection |
| **IP** | 1 minute | 200 requests | General protection |
| **IP** | 1 hour | 2,000 requests | Heavy usage prevention |
| **Client** | 1 minute | 500 requests | Fair usage |
| **Client** | 1 hour | 2,500 requests | Daily quotas |
| **Client** | 1 day | 25,000 requests | Overall limits |

### Endpoint-Specific Limits

| Endpoint | Per IP | Per Client | Special Rules |
|----------|--------|------------|---------------|
| `POST /api/v1/events` | 5/min | 20/min | Event creation |
| `POST /api/v1/*/reservations` | 10/min | 30/min | Reservation booking |
| `POST /api/v1/*/payments` | 3/min | 10/min | Payment processing |
| `GET /api/v1/public/events` | 100/min | 200/min | Public browsing |
| `GET /api/v1/public/events/search` | 30/min | 60/min | Search queries |

### Configuration File Structure

```json
{
  "RateLimiting": {
    "Enabled": true,
    "HttpStatusCode": 429,
    "QuotaExceededMessage": "API calls quota exceeded! Maximum allowed: {0} per {1}.",
    
    "IpRateLimit": {
      "Rules": [
        { "Period": "1s", "Limit": 10 },
        { "Period": "1m", "Limit": 200 },
        { "Period": "1h", "Limit": 2000 }
      ]
    },
    
    "ClientRateLimit": {
      "Rules": [
        { "Period": "1m", "Limit": 500 },
        { "Period": "1h", "Limit": 2500 },
        { "Period": "1d", "Limit": 25000 }
      ]
    },
    
    "EndpointRules": [
      {
        "Endpoint": "POST:/api/v1/events",
        "LimitPerIP": 5,
        "LimitPerClient": 20,
        "Period": "1m"
      }
    ],
    
    "IpWhitelist": ["127.0.0.1", "10.0.0.0/8"],
    "ClientWhitelist": ["admin-dashboard", "internal-monitoring"]
  }
}
```

## Usage Examples

### Basic API Usage

```bash
# Standard request with rate limiting
curl -X POST "https://api.example.com/api/v1/events" \
  -H "Content-Type: application/json" \
  -H "X-Client-Id: my-app-123" \
  -d '{"title": "My Event"}'

# Response includes rate limit headers:
# X-RateLimit-Limit: 500
# X-RateLimit-Remaining: 499
# X-RateLimit-Reset: 1673123456
# X-RateLimit-Period: 60
```

### Controller Attributes

```csharp
// Standard rate limiting
[RateLimit(50, "1m")] // 50 requests per minute
public async Task<ActionResult> StandardEndpoint() { ... }

// IP-based limiting  
[RateLimit(10, "1m", PerIP = true)]
public async Task<ActionResult> StrictEndpoint() { ... }

// Burst protection
[BurstProtection(10, "10s", 100, "1h")] // 10 per 10s, 100 per hour
public async Task<ActionResult> HighFrequencyEndpoint() { ... }

// Progressive limiting
[ProgressiveRateLimit(100, 50, 10)] // 100 → 50 → 10 on violations
public async Task<ActionResult> ProgressiveEndpoint() { ... }

// Bypass rate limiting
[NoRateLimit]
public async Task<ActionResult> UnrestrictedEndpoint() { ... }
```

### Admin Operations

```bash
# Check rate limit status
curl "https://api.example.com/api/v1/admin/rate-limit/status?clientId=user123"

# Clear rate limits
curl -X DELETE "https://api.example.com/api/v1/admin/rate-limit/clear?clientId=user123"

# Add to whitelist
curl -X POST "https://api.example.com/api/v1/admin/rate-limit/whitelist" \
  -H "Content-Type: application/json" \
  -d '{"clientId": "trusted-client", "duration": "24:00:00"}'

# Get metrics
curl "https://api.example.com/api/v1/admin/rate-limit/metrics?windowHours=24"
```

## Response Headers

All API responses include rate limiting information:

| Header | Description | Example |
|--------|-------------|---------|
| `X-RateLimit-Limit` | Maximum requests allowed | `500` |
| `X-RateLimit-Remaining` | Requests remaining in window | `485` |
| `X-RateLimit-Reset` | Unix timestamp when limit resets | `1673123456` |
| `X-RateLimit-Period` | Window duration in seconds | `60` |
| `Retry-After` | Seconds to wait before retry (if limited) | `45` |
| `X-RateLimit-Blocked` | Whether request was blocked | `true` |
| `X-RateLimit-Reason` | Reason for rate limiting | `IP rate limit exceeded` |

## Error Response Format

When rate limits are exceeded (HTTP 429):

```json
{
  "error": "rate_limit_exceeded",
  "message": "API calls quota exceeded! Maximum allowed: 500 per 1 minute.",
  "details": {
    "endpoint": "POST:/api/v1/events",
    "current_requests": 501,
    "max_requests": 500,
    "period_seconds": 60,
    "reset_time": "2024-01-15T10:35:00Z",
    "retry_after_seconds": 45,
    "reason": "Client rate limit exceeded"
  }
}
```

## Client Identification

Rate limits are applied based on client identification (in order of preference):

1. **X-Client-Id** header (preferred)
2. **X-API-Key** header
3. **client_id** JWT claim
4. **sub** JWT claim (user ID)
5. **IP + User-Agent hash** (fallback)

### Example Client Headers

```bash
# Best practice - explicit client ID
curl -H "X-Client-Id: my-mobile-app-v1.2.3"

# API key as client identifier
curl -H "X-API-Key: ak_1234567890abcdef"

# JWT token with client_id claim
curl -H "Authorization: Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiJ9..."
```

## Monitoring and Metrics

### Key Metrics

Track these metrics for optimal rate limiting:

```
# Request volume
rate_limit_requests_total{endpoint, client_id, status}

# Block rate
rate_limit_blocks_total{endpoint, client_id, reason}

# Current usage
rate_limit_current_usage{endpoint, client_id, window}

# Response times
rate_limit_check_duration_seconds
```

### Admin Dashboard Queries

```bash
# Get top blocked clients
curl "/api/v1/admin/rate-limit/stats?windowHours=24" | jq '.topClients'

# Monitor specific endpoint
curl "/api/v1/admin/rate-limit/metrics?endpoint=POST:/api/v1/events&windowHours=1"

# Real-time status check
curl "/api/v1/admin/rate-limit/status?clientId=suspicious-client"
```

### Alerts and Thresholds

Set up monitoring alerts for:

- **High block rate**: >5% of requests blocked
- **Suspicious patterns**: Single IP/client hitting multiple limits
- **Service degradation**: Rate limit check latency >100ms
- **Configuration issues**: Rate limit service errors

## Best Practices

### For API Consumers

1. **Include Client ID**: Always send `X-Client-Id` header
2. **Respect Rate Limits**: Check response headers and implement backoff
3. **Cache Responses**: Reduce unnecessary API calls
4. **Batch Operations**: Use bulk endpoints when available
5. **Implement Retry Logic**: Use exponential backoff with jitter

```javascript
// Example retry logic
async function apiCall(url, options, maxRetries = 3) {
  for (let attempt = 0; attempt < maxRetries; attempt++) {
    const response = await fetch(url, options);
    
    if (response.status !== 429) {
      return response;
    }
    
    const retryAfter = response.headers.get('Retry-After');
    const delay = retryAfter ? parseInt(retryAfter) * 1000 : Math.pow(2, attempt) * 1000;
    
    await new Promise(resolve => setTimeout(resolve, delay));
  }
  
  throw new Error('Rate limit exceeded after retries');
}
```

### For API Operators

1. **Monitor Patterns**: Watch for abuse and adjust limits
2. **Whitelist Partners**: Add trusted integrations to whitelist
3. **Progressive Enforcement**: Use graduated responses for violations
4. **Capacity Planning**: Set limits based on infrastructure capacity
5. **Emergency Controls**: Have procedures for rapid limit adjustment

### Rate Limit Strategy by Endpoint Type

| Endpoint Type | Strategy | Rationale |
|---------------|----------|-----------|
| **Authentication** | Strict per-IP | Prevent brute force attacks |
| **Public Read** | Generous limits | Encourage usage, cache-friendly |
| **User Actions** | Moderate per-user | Balance usability and protection |
| **Admin Operations** | Very strict | Protect sensitive functions |
| **Payment/Booking** | Ultra-strict | Prevent duplicate transactions |
| **Search/Filter** | Burst-aware | Handle user exploration patterns |

## Deployment Considerations

### Redis Configuration

For production deployments:

```yaml
# Redis cluster for high availability
redis:
  cluster:
    enabled: true
    nodes:
      - redis-1:6379
      - redis-2:6379
      - redis-3:6379
  
  # Connection pooling
  pool:
    maxActive: 100
    maxIdle: 50
    minIdle: 10
```

### Load Balancer Integration

Configure load balancers to pass real IP addresses:

```nginx
# Nginx configuration
location /api/ {
    proxy_pass http://backend;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
}
```

### Kubernetes Deployment

```yaml
# Rate limiting configuration
apiVersion: v1
kind: ConfigMap
metadata:
  name: rate-limit-config
data:
  appsettings.RateLimit.json: |
    {
      "RateLimiting": {
        "Enabled": true,
        "RedisConnectionString": "redis-cluster:6379"
      }
    }

---
# Application deployment with Redis dependency
apiVersion: apps/v1
kind: Deployment
metadata:
  name: event-api
spec:
  template:
    spec:
      containers:
      - name: event-api
        env:
        - name: REDIS_CONNECTION
          valueFrom:
            secretKeyRef:
              name: redis-secret
              key: connection-string
```

### Scaling Considerations

| Component | Scaling Strategy | Notes |
|-----------|------------------|-------|
| **API Instances** | Horizontal | Stateless rate limiting |
| **Redis** | Cluster mode | Distributed counters |
| **Rate Limit Service** | Per-instance | Embedded in API |
| **Admin Interface** | Single instance | Read-only operations |

## Troubleshooting

### Common Issues

#### 1. False Positives

**Symptoms**: Legitimate users being rate limited
**Causes**: Shared IP addresses (NAT, proxies)
**Solutions**:
- Implement client authentication
- Adjust IP-based limits
- Use organization-based limits

#### 2. Rate Limit Bypass

**Symptoms**: Abuse continuing despite rate limits
**Causes**: Multiple IP addresses, rotating clients
**Solutions**:
- Implement progressive enforcement
- Add behavior-based detection
- Use device fingerprinting

#### 3. Performance Impact

**Symptoms**: High latency on rate limit checks
**Causes**: Redis connectivity, complex rules
**Solutions**:
- Optimize Redis configuration
- Simplify rate limit rules
- Cache frequently accessed data

#### 4. Configuration Drift

**Symptoms**: Inconsistent behavior across instances
**Causes**: Different configuration files
**Solutions**:
- Centralize configuration management
- Use configuration validation
- Implement configuration monitoring

### Debugging Commands

```bash
# Check rate limit middleware
curl -H "X-Debug-Rate-Limit: true" "/api/v1/events"

# Test specific client
curl "/api/v1/admin/rate-limit/test" \
  -d '{"clientId":"test-client", "endpoint":"POST:/api/v1/events"}'

# Monitor Redis keys
redis-cli --scan --pattern "rate_limit:*"

# Check middleware registration
curl "/api/v1/health" -H "X-Debug: middleware"
```

### Log Analysis

Look for these log patterns:

```
# Normal operation
[INFO] Rate limit check passed for client abc123 on POST:/api/v1/events (150/500)

# Rate limit exceeded
[WARN] Rate limit exceeded for client abc123 from IP 192.168.1.100 on POST:/api/v1/events. Current: 501, Limit: 500

# Suspicious activity
[ERROR] Progressive rate limit triggered for client abc123 - multiple violations detected
```

## Security Considerations

### Protection Against Abuse

1. **DDoS Protection**: Layer with network-level protection
2. **Account Takeover**: Monitor unusual client behavior patterns
3. **API Scraping**: Detect automated access patterns
4. **Resource Exhaustion**: Limit expensive operations strictly

### Data Privacy

1. **IP Address Handling**: Follow GDPR requirements for IP logging
2. **Client Identification**: Avoid storing personally identifiable information
3. **Metrics Retention**: Implement data retention policies
4. **Access Logs**: Secure and limit access to rate limit logs

### Compliance

- **SOC 2**: Rate limiting as part of availability controls
- **PCI DSS**: Enhanced protection for payment endpoints
- **GDPR**: Data minimization in rate limit records
- **CCPA**: User rights regarding rate limit data

## Integration Examples

### With API Gateway

```csharp
// Pre-gateway rate limiting
app.UseMiddleware<AdvancedRateLimitMiddleware>();
app.UseMiddleware<ApiGatewayMiddleware>();

// Post-gateway rate limiting (application-specific)
app.UseAuthentication();
app.UseMiddleware<ApplicationRateLimitMiddleware>();
```

### With Circuit Breakers

```csharp
// Combine with circuit breaker for service protection
[RateLimit(100, "1m")]
[CircuitBreaker(failureThreshold: 50, timeoutSeconds: 30)]
public async Task<ActionResult> ProtectedEndpoint()
{
    // Implementation
}
```

### With Caching

```csharp
// Cache-aware rate limiting
[RateLimit(200, "1m")] // Higher limits for cached endpoints
[ResponseCache(Duration = 300)]
public async Task<ActionResult> CachedEndpoint()
{
    // Implementation with caching
}
```

## Performance Benchmarks

| Scenario | Throughput | Latency P99 | Notes |
|----------|------------|-------------|-------|
| **In-Memory** | 50K RPS | <1ms | Single instance |
| **Redis Local** | 30K RPS | <5ms | Local Redis |
| **Redis Cluster** | 25K RPS | <10ms | Distributed Redis |
| **Complex Rules** | 20K RPS | <15ms | Multiple endpoint rules |

## Migration Guide

### From AspNetCoreRateLimit

1. **Configuration**: Update JSON structure
2. **Middleware**: Replace middleware registration
3. **Custom Rules**: Migrate to new attribute system
4. **Storage**: Migrate Redis data if needed

### From Custom Implementation

1. **Assessment**: Audit existing rate limiting logic
2. **Mapping**: Map current rules to new configuration
3. **Testing**: Comprehensive testing with existing traffic patterns
4. **Rollout**: Gradual deployment with monitoring

---

## Support and Maintenance

### Regular Tasks

- **Weekly**: Review rate limit metrics and adjust if needed
- **Monthly**: Analyze blocked requests for patterns
- **Quarterly**: Update rate limits based on capacity changes
- **Annually**: Review and update rate limiting strategy

### Emergency Procedures

1. **Sudden Traffic Spike**: Implement emergency rate limits
2. **Service Degradation**: Disable non-essential endpoints
3. **Security Incident**: Implement strict emergency limits
4. **Configuration Error**: Rollback and validate changes

For additional support, refer to the main Event Service documentation or contact the development team.
