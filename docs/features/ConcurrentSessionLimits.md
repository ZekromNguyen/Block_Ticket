# Concurrent Session Limits Implementation

## Overview

The Concurrent Session Limits feature has been successfully implemented for the BlockTicket Identity Service. This feature allows administrators to configure and enforce limits on the number of concurrent sessions a user can have, enhancing security by preventing unauthorized account sharing and reducing the risk of compromised credentials.

## Features Implemented

### 1. Session Configuration
- **Configurable session limits** per user role
- **Flexible session limit behaviors**:
  - `RevokeOldest`: Automatically revoke oldest sessions when limit is exceeded
  - `RejectNew`: Reject new login attempts when limit is reached
  - `Unlimited`: Disable session limits entirely
- **Role-based session limits**: Different limits for different user types (Fan, Promoter, Admin)

### 2. Session Management Service
- **Session validation** before allowing new logins
- **Automatic session cleanup** when limits are exceeded
- **Token revocation** for ended sessions
- **Comprehensive session tracking** and monitoring

### 3. Session Management API
- **User session dashboard** endpoints for viewing active sessions
- **Individual session termination** capability
- **Bulk session management** (end all other sessions)
- **Session limit information** endpoint

## Configuration

### appsettings.json
```json
{
  "Security": {
    "MaxConcurrentSessions": 5,
    "SessionLimitBehavior": "RevokeOldest",
    "EnableSessionLimits": true
  }
}
```

### Role-Based Limits
- **Regular Users (Fan)**: Base limit (5 sessions)
- **Promoters**: 1.5x base limit (7 sessions)
- **Admins/Super Admins**: 2x base limit (10 sessions)

## API Endpoints

### Session Management Controller

#### GET `/api/v1/sessions/limits`
Returns session limit information for the current user:
```json
{
  "maxAllowedSessions": 5,
  "currentActiveSessions": 3,
  "canCreateNewSession": true,
  "limitBehavior": "RevokeOldest",
  "activeSessions": [...]
}
```

#### GET `/api/v1/sessions/active`
Returns list of active sessions for the current user:
```json
[
  {
    "id": "guid",
    "deviceInfo": "Chrome on Windows",
    "ipAddress": "192.168.1.100",
    "createdAt": "2025-01-01T10:00:00Z",
    "expiresAt": "2025-01-02T10:00:00Z",
    "isActive": true,
    "isCurrentSession": false
  }
]
```

#### DELETE `/api/v1/sessions/{sessionId}`
Ends a specific session.

#### DELETE `/api/v1/sessions/others`
Ends all other sessions (keeps current session active).

## Files Created/Modified

### New Files
1. `Identity.Domain/Configuration/SessionConfiguration.cs` - Configuration model
2. `Identity.Domain/Services/ISessionManagementService.cs` - Service interface
3. `Identity.Infrastructure/Services/SessionManagementService.cs` - Service implementation
4. `Identity.Domain/Exceptions/SessionLimitExceededException.cs` - Custom exception
5. `Identity.Application/DTOs/SessionManagementDtos.cs` - DTOs for session management
6. `Identity.API/Controllers/V1/SessionManagementController.cs` - API controller
7. `Identity.Tests/Unit/Services/SessionManagementServiceTests.cs` - Unit tests

### Modified Files
1. `appsettings.json` - Added session configuration
2. `appsettings.Development.json` - Development-specific settings
3. `IUserSessionRepository.cs` - Added session count methods
4. `UserSessionRepository.cs` - Implemented new methods
5. `LoginCommand.cs` - Added session limit checks
6. `UserDomainEvents.cs` - Added session-related domain events
7. `User.cs` - Updated CreateSession method
8. `DependencyInjection.cs` - Registered session management service

## Behavior Details

### Login Flow with Session Limits

1. **User attempts login**
2. **Authentication validates credentials**
3. **Session service checks current active sessions**
4. **If within limits**: Session created normally
5. **If at limit with RevokeOldest behavior**: 
   - Oldest sessions are automatically revoked
   - Associated tokens are invalidated
   - New session is created
6. **If at limit with RejectNew behavior**: 
   - Login is rejected with appropriate error message
   - User must manually end existing sessions

### Session Cleanup Process

When sessions are revoked due to limits:
1. Session is marked as ended (`EndedAt` timestamp set)
2. Refresh token is revoked from session
3. All associated reference tokens are revoked
4. Audit log entry is created
5. Domain events are published

## Security Considerations

1. **Token Security**: All tokens associated with revoked sessions are immediately invalidated
2. **Audit Logging**: All session creation, revocation, and limit enforcement events are logged
3. **User Notification**: Users can view their active sessions and manage them
4. **Graceful Degradation**: If session service fails, system defaults to allowing login (fail-open for availability)

## Monitoring and Metrics

### Domain Events Published
- `UserSessionCreatedDomainEvent`: When new session is created
- `UserSessionEndedDomainEvent`: When session is manually or automatically ended
- `UserSessionLimitExceededDomainEvent`: When user attempts exceed limits

### Audit Logs
- Session creation events
- Session termination events (manual and automatic)
- Session limit enforcement actions
- Token revocation events

## Testing

### Unit Tests Implemented
- Session limit validation
- Active session counting
- Role-based limit calculation
- Session revocation logic

### Integration Testing Recommendations
1. Test with multiple concurrent logins
2. Verify token invalidation on session revocation
3. Test role-based limit differences
4. Verify audit log creation
5. Test configuration changes

## Configuration Examples

### Production Settings
```json
{
  "Security": {
    "MaxConcurrentSessions": 5,
    "SessionLimitBehavior": "RevokeOldest"
  }
}
```

### Development Settings
```json
{
  "Security": {
    "MaxConcurrentSessions": 3,
    "SessionLimitBehavior": "RevokeOldest"
  }
}
```

### High Security Environment
```json
{
  "Security": {
    "MaxConcurrentSessions": 2,
    "SessionLimitBehavior": "RejectNew"
  }
}
```

## Future Enhancements

1. **Device Fingerprinting**: More sophisticated device identification
2. **Geolocation Validation**: Block sessions from suspicious locations
3. **Session Analytics**: Detailed session usage analytics
4. **Push Notifications**: Real-time notifications for new sessions
5. **Session Policies**: More granular session management policies
6. **Admin Override**: Admin ability to bypass session limits for specific users

## Troubleshooting

### Common Issues

1. **Sessions not being revoked**: Check token service configuration
2. **Incorrect session counts**: Verify database connection and session cleanup service
3. **Configuration not applied**: Ensure appsettings.json is properly formatted
4. **Role-based limits not working**: Check user role assignments and service registration

### Debug Logging

Enable detailed logging for session management:
```json
{
  "Logging": {
    "LogLevel": {
      "Identity.Infrastructure.Services.SessionManagementService": "Debug"
    }
  }
}
```

## Conclusion

The Concurrent Session Limits feature is now fully implemented and provides a robust, secure, and configurable solution for managing user sessions. The implementation follows clean architecture principles, includes comprehensive error handling, and provides excellent observability through logging and domain events.
