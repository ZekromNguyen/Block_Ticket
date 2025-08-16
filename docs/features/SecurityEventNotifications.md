# Security Event Notifications with Discord Integration

## Overview

The Security Event Notifications system provides real-time alerting for security events through multiple channels, with primary focus on Discord integration. This system automatically monitors security events, suspicious activities, and account lockouts, sending immediate notifications to designated Discord channels based on severity and type.

## Architecture

### Core Components

#### 1. Notification Services
- **ISecurityNotificationService**: Main orchestrator for all security notifications
- **IDiscordNotificationService**: Specialized service for Discord webhook integration
- **SecurityNotificationSchedulerService**: Background service for scheduled summaries

#### 2. Event Handlers
- **SecurityEventNotificationHandler**: Processes domain events and triggers notifications
- **SecurityMonitoringService**: Enhanced to send notifications for detected threats

#### 3. Configuration System
- **SecurityNotificationConfig**: Configurable notification rules and channels
- **NotificationChannel**: Multi-channel support (Discord, Email, SMS)
- **NotificationThrottling**: Rate limiting to prevent notification spam

## Features

### Real-Time Security Alerts

#### Critical Events (Immediate Notification)
- Account lockouts
- Permission violations
- Suspicious login attempts
- Multiple failed login attempts
- Rate limit violations
- MFA enable/disable events

#### High Severity Events
- Password changes
- Unusual location access
- Brute force attack detection
- Account enumeration attempts

#### Medium Severity Events
- Failed login attempts (configurable threshold)
- Session anomalies
- Configuration changes

### Notification Channels

#### Discord Integration
- **Rich Formatting**: Color-coded embeds based on severity
- **Structured Data**: Organized fields for easy reading
- **Webhook Routing**: Different webhooks for different severity levels
- **Interactive Elements**: Timestamps, avatars, and formatting

#### Email Notifications
- Security alerts to administrators
- User notifications for account-related events
- HTML formatted messages

#### SMS Notifications
- Critical alerts for immediate attention
- Concise messages for mobile devices

### Daily Security Summaries

Automated daily reports including:
- Total security events breakdown
- Unresolved events count
- Account lockout statistics
- Top event types and source IPs
- Suspicious activity summary
- Failed vs successful login ratios

## Configuration

### Basic Discord Setup

```json
{
  "Notifications": {
    "Discord": {
      "DefaultWebhookUrl": "https://discord.com/api/webhooks/YOUR_WEBHOOK_ID/YOUR_WEBHOOK_TOKEN",
      "CriticalWebhookUrl": "https://discord.com/api/webhooks/YOUR_CRITICAL_WEBHOOK_ID/YOUR_CRITICAL_WEBHOOK_TOKEN"
    }
  }
}
```

### Advanced Configuration

```json
{
  "Notifications": {
    "SecurityEvents": {
      "Enabled": true,
      "Channels": [
        {
          "Name": "default-discord",
          "Type": "discord",
          "Target": "default-webhook",
          "Severities": ["Medium", "High", "Critical"],
          "EventTypes": ["*"],
          "Enabled": true
        },
        {
          "Name": "critical-discord",
          "Type": "discord",
          "Target": "critical-webhook",
          "Severities": ["Critical"],
          "EventTypes": ["*"],
          "Enabled": true
        },
        {
          "Name": "admin-email",
          "Type": "email",
          "Target": "security@company.com",
          "Severities": ["Critical"],
          "EventTypes": ["ACCOUNT_LOCKED", "PERMISSION_DENIED"],
          "Enabled": true
        }
      ],
      "Rules": [
        {
          "Name": "critical-events",
          "EventTypes": ["*"],
          "Severities": ["Critical"],
          "Channels": ["critical-discord", "admin-email"],
          "Enabled": true
        },
        {
          "Name": "brute-force-detection",
          "EventTypes": ["MULTIPLE_FAILED_LOGINS", "BRUTE_FORCE_ATTACK"],
          "Severities": ["High", "Critical"],
          "Channels": ["default-discord"],
          "Condition": {
            "MinOccurrences": 3,
            "TimeWindow": "00:05:00"
          },
          "Enabled": true
        }
      ],
      "Throttling": {
        "Enabled": true,
        "Window": "00:05:00",
        "MaxNotifications": 10,
        "PerEventTypeLimits": {
          "LOGIN_FAILURE": 5,
          "ACCOUNT_LOCKED": 3,
          "SUSPICIOUS_LOGIN": 8
        }
      }
    },
    "DailySummaryTime": "09:00"
  }
}
```

## API Endpoints

### Security Notification Controller

#### Send Test Notification
```http
POST /api/security-notifications/test?message=Hello
Authorization: Bearer {token}
```

#### Send Critical Alert
```http
POST /api/security-notifications/critical-alert
Content-Type: application/json
Authorization: Bearer {token}

{
  "message": "Security breach detected",
  "context": "Unauthorized access from unknown IP"
}
```

#### Generate Security Summary
```http
POST /api/security-notifications/summary?from=2025-08-15&to=2025-08-16
Authorization: Bearer {token}
```

#### Get Recent Notifiable Events
```http
GET /api/security-notifications/recent-events?hours=24
Authorization: Bearer {token}
```

#### Test Security Alert Format
```http
POST /api/security-notifications/test-security-alert
Authorization: Bearer {token}
```

#### Check Notification Status
```http
GET /api/security-notifications/status
Authorization: Bearer {token}
```

## Discord Webhook Setup

### 1. Create Discord Server
1. Create a new Discord server or use an existing one
2. Create dedicated channels for security alerts (e.g., #security-alerts, #critical-alerts)

### 2. Create Webhooks
1. In your Discord server, go to Server Settings > Integrations
2. Click "Create Webhook"
3. Choose the channel (e.g., #security-alerts)
4. Name your webhook (e.g., "BlockTicket Security Bot")
5. Copy the webhook URL
6. Repeat for critical alerts channel

### 3. Configure Multiple Webhooks
1. Create separate webhooks for different severity levels
2. Update appsettings.json with both webhook URLs

### 4. Customize Webhook Appearance
1. Upload an avatar for your security bot
2. Set display name and description

## Message Formats

### Security Event Alert
```
🚨 Security Event: ACCOUNT_LOCKED

Account locked: Too many failed login attempts

Event Type: ACCOUNT_LOCKED          Category: SECURITY
Severity: High                      IP Address: 192.168.1.100
User ID: 12345678-1234-1234-1234-123456789012
Timestamp: 2025-08-16 14:30:00 UTC

User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36

BlockTicket Security System
```

### Suspicious Activity Alert
```
⚠️ Suspicious Activity: BRUTE_FORCE_ATTACK

Multiple failed login attempts detected

Activity Type: BRUTE_FORCE_ATTACK   Risk Score: 85.0
IP Address: 192.168.1.100          Status: Detected
User ID: 12345678-1234-1234-1234-123456789012
Timestamp: 2025-08-16 14:30:00 UTC

User Agent: curl/7.68.0

BlockTicket Security System
```

### Daily Summary
```
📊 Daily Security Summary - 2025-08-16

Security events summary for the past 24 hours

Total Events: 147              Critical: 2
High Severity: 18             Medium Severity: 42
Low Severity: 85              Unresolved: 8
Account Lockouts: 3           Suspicious Activities: 12
Failed Logins: 28             Successful Logins: 145

Top Event Types: LOGIN_SUCCESS (145), LOGIN_FAILURE (28), ACCOUNT_LOCKED (3)
Top Source IPs: 192.168.1.100 (25), 10.0.0.50 (18), 203.0.113.42 (12)

BlockTicket Security System
```

## Integration Points

### Automatic Event Processing
- Security events are automatically processed when logged
- Domain event handlers trigger notifications
- Background monitoring service sends real-time alerts

### Throttling and Rate Limiting
- Prevents notification spam
- Configurable limits per event type
- Time-window based throttling

### Multi-Channel Support
- Simultaneous delivery to multiple channels
- Channel-specific formatting
- Fallback mechanisms

## Security Considerations

### Sensitive Data Protection
- No passwords or tokens in notifications
- User IDs obfuscated when necessary
- IP addresses and locations included for context

### Access Control
- Admin-only access to notification controls
- Secure webhook URL management
- Audit logging for notification activities

### Monitoring and Alerting
- Self-monitoring for notification failures
- Backup notification channels
- Health checks for external services

## Testing

### Unit Tests
- Comprehensive test coverage for all services
- Mock HTTP clients for Discord integration
- Configuration testing
- Throttling mechanism validation

### Integration Tests
- End-to-end notification flow testing
- Multiple channel delivery verification
- Error handling and retry logic

### Manual Testing
Use the provided API endpoints to test:
1. Basic connectivity: `/test`
2. Alert formatting: `/test-security-alert`
3. Critical alerts: `/critical-alert`
4. Summary generation: `/summary`

## Performance Considerations

### Asynchronous Processing
- Non-blocking notification sending
- Background task processing
- Separate thread pools for notifications

### Caching and Throttling
- Redis-based throttling cache
- Efficient rate limiting algorithms
- Memory-efficient message queuing

### Error Handling
- Graceful degradation on service failures
- Retry mechanisms with exponential backoff
- Circuit breaker patterns for external services

## Monitoring and Observability

### Metrics
- Notification success/failure rates
- Response times for external services
- Event processing throughput
- Throttling statistics

### Logging
- Structured logging for all operations
- Correlation IDs for tracking
- Performance metrics
- Error details and stack traces

### Health Checks
- Discord webhook connectivity
- Configuration validation
- Service dependency checks

## Deployment

### Prerequisites
- Discord server with webhook creation permissions
- Webhook URLs configured
- Proper service registrations in DI container

### Configuration Deployment
1. Update appsettings.json with webhook URLs
2. Configure notification channels and rules
3. Set appropriate throttling limits
4. Configure summary schedule

### Verification
1. Check service status: `/api/security-notifications/status`
2. Send test notification: `/api/security-notifications/test`
3. Verify webhook connectivity
4. Monitor logs for any errors

## Troubleshooting

### Common Issues

#### Notifications Not Sending
- Verify webhook URLs are correct
- Check Discord server permissions
- Validate configuration format
- Review throttling settings

#### Formatting Issues
- Check JSON serialization settings
- Verify Discord embed format
- Test with simple messages first

#### Performance Problems
- Monitor throttling cache
- Check background service logs
- Verify async task completion

### Debug Steps
1. Enable debug logging
2. Test individual components
3. Check external service connectivity
4. Validate configuration parsing

## Future Enhancements

### Planned Features
- Microsoft Teams integration
- Custom webhook endpoints
- Advanced filtering rules
- Machine learning-based alert prioritization

### Potential Improvements
- Interactive Discord buttons
- Real-time dashboard integration
- Mobile push notifications
- Integration with SIEM systems

---

**Last Updated**: August 16, 2025  
**Version**: 2.0.0  
**Maintainer**: Identity Service Team