# Block Ticket Platform - Microservices Project Guidelines

## Project Overview
Block Ticket is a comprehensive event ticketing platform built using .NET 9 microservices architecture with Clean Architecture principles. The platform handles the complete event lifecycle from creation to verification, including user management, payment processing, blockchain integration, and resale operations.

## Microservices Architecture

### Service Portfolio
```
src/
├── Services/
│   ├── Identity/            # User management & authentication (Port 5001)
│   │   ├── Identity.API/    # Authentication endpoints & OpenIddict
│   │   ├── Identity.Application/  # User business logic
│   │   ├── Identity.Domain/ # User entities & domain rules
│   │   └── Identity.Infrastructure/ # Data access & external auth
│   ├── Event/               # Event management (Port 5002)
│   │   ├── Event.API/       # Event & venue endpoints
│   │   ├── Event.Application/ # Event business logic
│   │   ├── Event.Domain/    # Event entities & rules
│   │   └── Event.Infrastructure/ # Data access & caching
│   ├── Ticketing/           # Ticket purchasing logic (Port 5003)
│   │   ├── Ticketing.API/   # Purchase & reservation endpoints
│   │   ├── Ticketing.Application/ # Ticketing business logic
│   │   ├── Ticketing.Domain/ # Ticket entities & rules
│   │   └── Ticketing.Infrastructure/ # Payment & inventory
│   ├── Payment/             # Payment processing
│   │   ├── Payment.API/     # Payment endpoints
│   │   ├── Payment.Application/ # Payment logic
│   │   ├── Payment.Domain/  # Payment entities
│   │   └── Payment.Infrastructure/ # Payment gateways
│   ├── Notification/        # Notification service (Worker)
│   │   ├── Notification.API/ # Notification endpoints
│   │   ├── Notification.Application/ # Message logic
│   │   ├── Notification.Domain/ # Message entities
│   │   └── Notification.Infrastructure/ # Email/SMS providers
│   ├── Resale/              # Ticket resale & waiting list (Port 5004)
│   ├── Verification/        # Ticket verification (Port 5005)
│   └── BlockchainOrchestrator/ # Blockchain operations (Worker)
├── ApiGateway/              # YARP reverse proxy (Port 5000)
├── Gateway/                 # Alternative gateway implementation
└── Shared/
    ├── Common/              # Shared utilities & base classes
    └── Contracts/           # Events, Commands & DTOs
```

### Clean Architecture per Service
Each microservice follows Clean Architecture:
- **API Layer**: Controllers, middleware, configuration
- **Application Layer**: Use cases, interfaces, DTOs, validation
- **Domain Layer**: Entities, value objects, domain services, events
- **Infrastructure Layer**: Repositories, external services, data access

### Dependency Flow
- API → Application → Domain
- Infrastructure → Application, Domain
- No circular dependencies between layers
- Domain layer has no external dependencies
- Services communicate via message bus (MassTransit/RabbitMQ)

## Technology Stack

### Core Technologies
- **.NET 9**: Latest LTS version for performance and features
- **ASP.NET Core**: Web API framework for all services
- **Entity Framework Core**: ORM for data access
- **PostgreSQL**: Primary database for each service
- **Redis**: Distributed caching and session storage
- **RabbitMQ**: Message broker for inter-service communication
- **MassTransit**: Message broker abstraction layer

### Infrastructure & Deployment
- **Docker**: Containerization for all services
- **Kubernetes**: Container orchestration (k8s manifests included)
- **YARP**: Reverse proxy for API Gateway
- **OpenIddict**: Authentication server in Identity Service
- **Ganache**: Local blockchain development environment
- **Prometheus & Grafana**: Monitoring and visualization

### Key Packages per Service
- **MediatR**: Command/Query handling pattern
- **FluentValidation**: Input validation
- **AutoMapper**: Object-to-object mapping
- **Serilog**: Structured logging across all services
- **OpenTelemetry**: Observability and distributed tracing
- **Swashbuckle**: API documentation for each service

## Service Communication

### API Gateway (Port 5000)
- **YARP Reverse Proxy**: Routes requests to appropriate services
- **Authentication**: Validates JWT tokens from Identity Service
- **Swagger Aggregation**: Unified API documentation
- **CORS Configuration**: Cross-origin request handling
- **Rate Limiting**: Request throttling and protection

### Identity Service (Port 5001)
- **OpenIddict Integration**: OAuth2/OpenID Connect server
- **User Management**: Registration, authentication, profile management
- **Multi-Factor Authentication**: TOTP and SMS verification
- **Role-Based Access Control**: Admin, promoter, user roles
- **JWT Token Management**: Access and refresh token handling
- **Security Features**: Password policies, session management

### Event Service (Port 5002)
- **Event Management**: CRUD operations for events
- **Venue Management**: Location and capacity handling
- **Seat Map Management**: Complex seating arrangements
- **Pricing Rules**: Dynamic and time-based pricing
- **Allocation Management**: Ticket inventory control
- **Search & Catalog**: Event discovery and filtering
- **Approval Workflows**: Event publishing approval process

### Ticketing Service (Port 5003)
- **Ticket Purchasing**: Purchase flow and payment integration
- **Reservation System**: Temporary seat holds with TTL
- **Inventory Management**: Real-time availability tracking
- **Queue Management**: High-demand event handling
- **Transaction Processing**: Atomic purchase operations

### Message Bus Integration
All services use **MassTransit** with **RabbitMQ** for:
- **Event Publishing**: Domain events to integration events
- **Service Coordination**: Cross-service workflows
- **Eventual Consistency**: Distributed transaction patterns
- **Retry Policies**: Resilient message handling
- **Dead Letter Queues**: Failed message management

## Domain Model Guidelines

### Identity Service Domain
**Core Entities:**
- **User**: Account management, authentication, profiles
- **Role**: Permission-based access control
- **Session**: User session tracking and management
- **MfaToken**: Multi-factor authentication handling

### Event Service Domain  
**Core Entities:**
1. **EventAggregate**: Root entity for event management
2. **Venue**: Physical or virtual event locations with complex layouts
3. **TicketType**: Different ticket categories and pricing tiers
4. **Allocation**: Inventory management and access control
5. **Reservation**: Temporary ticket holds with TTL
6. **PricingRule**: Dynamic pricing based on time and demand
7. **EventSeries**: Recurring event management
8. **ApprovalWorkflow**: Event publishing approval process

### Shared Value Objects
- **Money**: Currency-aware monetary values with proper arithmetic
- **Slug**: SEO-friendly URL identifiers
- **TimeZoneId**: Timezone handling for global events
- **GeoCoordinates**: Location data for venues
- **DateTimeRange**: Time period representation
- **SeatMapSchema**: Complex seating arrangement definitions

### Cross-Service Business Rules
- Events must have future dates when created
- User authentication required for all operations
- Venue capacity cannot be exceeded across all ticket types
- Ticket reservations have automatic expiration
- Payment processing must be atomic
- Inventory tracking must be eventually consistent across services
- Blockchain transactions are immutable once confirmed

## API Design Standards

### Service-Specific Endpoints

#### Identity Service (Port 5001)
```
POST   /api/auth/register             # User registration
POST   /api/auth/login                # User authentication
POST   /api/auth/refresh              # Token refresh
POST   /api/auth/logout               # User logout
GET    /api/users/profile             # Get user profile
PUT    /api/users/profile             # Update user profile
POST   /api/auth/mfa/setup            # Setup 2FA
POST   /api/auth/mfa/verify           # Verify 2FA token
```

#### Event Service (Port 5002)
```
GET    /api/v1/events                 # List events (paginated)
POST   /api/v1/events                 # Create new event
GET    /api/v1/events/{id}            # Get event details
PUT    /api/v1/events/{id}            # Update event
DELETE /api/v1/events/{id}            # Cancel event
GET    /api/v1/events/by-slug/{orgId}/{slug}  # Get by slug
POST   /api/v1/events/{id}/publish    # Publish event
GET    /api/v1/venues                 # List venues
POST   /api/v1/venues                 # Create venue
```

#### Ticketing Service (Port 5003)
```
POST   /api/tickets/reserve           # Create ticket reservation
POST   /api/tickets/purchase          # Complete ticket purchase
GET    /api/tickets/{id}              # Get ticket details
POST   /api/tickets/{id}/transfer     # Transfer ticket ownership
GET    /api/availability/{eventId}    # Check seat availability
```

#### API Gateway (Port 5000)
- **Route Aggregation**: Unified entry point for all services
- **Service Discovery**: Automatic routing to healthy service instances
- **Authentication Middleware**: JWT token validation
- **Rate Limiting**: Per-user and per-endpoint throttling

### Response Formats
- **Success**: 200/201/204 with appropriate data and correlation ID
- **Validation**: 400 with detailed field-level error messages
- **Authentication**: 401 with token refresh instructions
- **Authorization**: 403 with required permission details
- **Not Found**: 404 with resource information
- **Conflict**: 409 for business rule violations (e.g., seat already taken)
- **Server Error**: 500 with correlation ID for troubleshooting

### Distributed API Patterns
- **Correlation IDs**: Track requests across multiple services
- **API Versioning**: URL-based versioning for all services
- **Pagination**: Cursor-based pagination for large datasets
- **Circuit Breakers**: Handle downstream service failures gracefully
- **Retry Policies**: Automatic retry with exponential backoff

## Database Design

### Per-Service Database Strategy
Each microservice maintains its own PostgreSQL database:

#### Identity Database
- **users**: Core user accounts and authentication
- **roles**: Role definitions and permissions
- **user_roles**: User-role mappings
- **sessions**: Active user sessions
- **mfa_tokens**: Multi-factor authentication tokens
- **oauth_applications**: OpenIddict OAuth applications
- **oauth_tokens**: Access and refresh tokens

#### Event Database
- **events**: Core event information
- **venues**: Location and facility data with PostGIS
- **seat_maps**: Complex seating arrangement schemas
- **ticket_types**: Pricing and availability per event
- **allocations**: Inventory management and access control
- **reservations**: Temporary holds with TTL
- **pricing_rules**: Dynamic pricing logic
- **approval_workflows**: Event publishing workflows
- **audit_logs**: Change tracking and compliance

#### Ticketing Database
- **tickets**: Purchased ticket records
- **purchases**: Transaction and payment data
- **inventory**: Real-time seat availability
- **queues**: High-demand event queue management
- **transfers**: Ticket ownership changes

### Cross-Service Data Consistency
- **Event Sourcing**: Critical operations use event sourcing
- **Saga Pattern**: Distributed transactions across services
- **Eventual Consistency**: Accept temporary inconsistency for performance
- **Compensation Actions**: Rollback mechanisms for failed operations

### Indexing Strategy per Service
- **Primary Keys**: B-tree indexes for all entities
- **Foreign Keys**: B-tree indexes for cross-table joins
- **Search Fields**: GIN indexes for full-text search in events
- **Date Ranges**: B-tree indexes for temporal queries
- **Geospatial**: PostGIS indexes for venue location queries
- **User Sessions**: Hash indexes for session lookups
- **Inventory Queries**: Composite indexes for availability checks

### Data Integrity & Constraints
- **Foreign Key Constraints**: Within service boundaries only
- **Check Constraints**: Business rule enforcement at DB level
- **Unique Constraints**: Natural keys and business identifiers
- **Cascade Rules**: Proper cleanup for entity deletions
- **Database-Level Validation**: Critical business rules enforced in DB

## Caching Strategy

### Multi-Level Caching Architecture

#### Service-Level Caching
1. **Application Cache**: In-memory caching within each service
2. **Distributed Cache**: Redis for shared state across service instances
3. **Database Cache**: PostgreSQL query result caching per service
4. **API Gateway Cache**: Response caching at the gateway level
5. **CDN Cache**: Static content and public API responses

#### Cache Keys by Service
**Identity Service:**
- `identity:user:{userId}` - User profile and permissions
- `identity:session:{sessionId}` - Active user sessions
- `identity:roles:{userId}` - User role assignments

**Event Service:**
- `event:{organizationId}:{eventId}` - Event details and metadata
- `venue:{venueId}` - Venue information and layout
- `search:{hash}` - Event search results
- `seatmap:{eventId}` - Seat layout and availability overview

**Ticketing Service:**
- `availability:{eventId}` - Real-time seat availability
- `pricing:{eventId}:{timestamp}` - Current pricing rules
- `inventory:{eventId}:{sectionId}` - Section-level availability

#### TTL Strategy by Data Type
- **Static Data**: 1 hour (venues, organizations, user profiles)
- **Semi-Static Data**: 15 minutes (event details, pricing rules)
- **Dynamic Data**: 2 minutes (seat availability, current prices)
- **Real-time Data**: 30 seconds (active reservations, queue status)
- **User Sessions**: 30 minutes sliding expiration
- **Search Results**: 5 minutes (varies by query complexity)

#### Cache Invalidation Patterns
- **Event-Driven**: Domain events trigger cache invalidation
- **Time-Based**: TTL expiration for predictable refresh cycles
- **Manual**: Admin operations can force cache refresh
- **Cascade**: Related data invalidation (event → tickets → pricing)

#### Cache-Aside Pattern Implementation
```csharp
// Example from Event Service
public async Task<EventDto> GetEventAsync(Guid eventId)
{
    var cacheKey = $"event:{eventId}";
    var cached = await _cache.GetAsync<EventDto>(cacheKey);
    
    if (cached != null) return cached;
    
    var eventData = await _repository.GetByIdAsync(eventId);
    var dto = _mapper.Map<EventDto>(eventData);
    
    await _cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(15));
    return dto;
}
```

#### Performance Monitoring
- **Cache Hit Ratio**: Target >90% for frequently accessed data
- **Cache Miss Latency**: Monitor database query performance on misses
- **Eviction Rates**: Track memory pressure and optimize TTL values
- **Distributed Cache Latency**: Redis response time monitoring

## Security Implementation

### Multi-Service Authentication Architecture

#### Identity Service (Authentication Server)
- **OpenIddict Integration**: Full OAuth2/OpenID Connect server
- **JWT Token Management**: Access tokens (15 min) and refresh tokens (7 days)
- **Multi-Factor Authentication**: TOTP, SMS, and email verification
- **Password Policies**: Complexity requirements and history enforcement
- **Account Security**: Lockout policies, suspicious activity detection

#### API Gateway Security
- **JWT Validation**: Centralized token verification for all services
- **Rate Limiting**: Global and per-endpoint throttling
- **CORS Management**: Cross-origin request handling
- **Request Sanitization**: Input validation and XSS protection

#### Service-to-Service Authentication
- **API Keys**: Service authentication for internal communication
- **Mutual TLS**: Certificate-based authentication for sensitive operations
- **Message Signing**: HMAC signatures for message bus communication

### Authorization Framework

#### Role-Based Access Control (RBAC)
```csharp
// Example roles and permissions
public static class Roles
{
    public const string SuperAdmin = "super_admin";
    public const string Admin = "admin"; 
    public const string Promoter = "promoter";
    public const string User = "user";
}

public static class Permissions
{
    public const string CreateEvent = "events:create";
    public const string ManageVenue = "venues:manage";
    public const string ViewReports = "reports:view";
    public const string ProcessRefunds = "payments:refund";
}
```

#### Resource-Level Security
- **Organization Scoping**: Users can only access their organization's data
- **Event-Level Permissions**: Granular access to specific events
- **Data Isolation**: Multi-tenant data separation
- **Audit Trails**: All data modifications logged with user context

### Data Protection Standards

#### Encryption at Rest
- **Database Encryption**: PostgreSQL TDE for sensitive data
- **PII Encryption**: Additional encryption for personal information
- **Key Management**: Azure Key Vault or similar for key rotation
- **Backup Encryption**: Encrypted database backups

#### Encryption in Transit
- **TLS 1.3**: All service-to-service communication
- **Certificate Management**: Automated certificate rotation
- **API Gateway**: SSL termination and re-encryption

#### Sensitive Data Handling
```csharp
// Example of PII handling
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; }
    
    [Encrypted] // Custom attribute for automatic encryption
    public string PhoneNumber { get; set; }
    
    [Hashed] // One-way hashing for non-reversible data
    public string PasswordHash { get; set; }
}
```

### Security Monitoring & Compliance

#### Security Event Logging
- **Authentication Events**: Login attempts, failures, lockouts
- **Authorization Events**: Permission denials, privilege escalations
- **Data Access**: PII access, bulk data exports
- **Administrative Actions**: User management, system configuration

#### Threat Detection
- **Failed Login Monitoring**: Brute force attack detection
- **Anomaly Detection**: Unusual user behavior patterns
- **Rate Limit Violations**: Potential DDoS or abuse attempts
- **SQL Injection Attempts**: Database attack monitoring

#### Compliance Requirements
- **GDPR Compliance**: Right to deletion, data portability
- **PCI DSS**: Payment card data protection (Payment Service)
- **SOC 2**: Security and availability controls
- **Data Retention**: Automated cleanup of expired data

## Testing Strategy

### Microservices Testing Pyramid
```
E2E Tests (5%)          # Full workflow across multiple services
Integration (25%)       # Service + Database + Message Bus tests  
Contract Tests (15%)    # Service-to-service API contract validation
Unit Tests (55%)        # Business logic & domain tests per service
```

### Service-Specific Testing

#### Identity Service Testing
- **Authentication Flows**: Login, registration, token refresh
- **Authorization**: Role-based access control validation
- **Security**: Password policies, MFA, account lockout
- **Integration**: OpenIddict configuration and OAuth flows

#### Event Service Testing
- **Domain Logic**: Event creation, validation, status transitions
- **Complex Scenarios**: Seat map validation, pricing rule evaluation
- **Performance**: High-volume event search and filtering
- **Caching**: Cache hit/miss scenarios and invalidation

#### Inter-Service Testing
- **Message Bus**: Event publishing and consumption across services
- **API Contracts**: Consumer-driven contract testing with Pact
- **Eventual Consistency**: Distributed transaction scenarios
- **Failure Scenarios**: Service unavailability and circuit breaker testing

### Test Categories by Layer

#### Unit Tests (55% - ~2000+ tests)
```csharp
// Example domain test
[Test]
public void EventAggregate_Should_Prevent_Overbooking()
{
    // Given
    var eventAggregate = EventAggregate.Create(/*...*/);
    var venueCapacity = 100;
    
    // When & Then
    Assert.Throws<DomainException>(() => 
        eventAggregate.CreateTicketType("VIP", 150)); // Exceeds capacity
}
```

#### Integration Tests (25% - ~800+ tests)
- **Repository Tests**: Database operations with test containers
- **Controller Tests**: HTTP endpoints with test database
- **Message Handler Tests**: Event processing and side effects
- **Cache Integration**: Redis cache behavior validation

#### Contract Tests (15% - ~400+ tests)
- **Consumer Contracts**: API expectations from consuming services
- **Provider Verification**: Ensure API changes don't break consumers
- **Message Contracts**: Event schema validation across services
- **Version Compatibility**: API versioning and backward compatibility

#### End-to-End Tests (5% - ~150+ tests)
- **User Journeys**: Complete ticket purchase flow
- **Cross-Service Workflows**: Event creation → ticket purchase → verification
- **Error Scenarios**: Payment failures, service outages
- **Performance**: Load testing critical user paths

### Test Infrastructure

#### Test Data Management
- **Test Containers**: Isolated PostgreSQL and Redis for integration tests
- **Data Builders**: Fluent test data creation with realistic relationships
- **Seed Data**: Consistent test data across different test suites
- **Data Cleanup**: Automatic cleanup between test runs

#### Test Environment Strategy
```yaml
# Example docker-compose.test.yml
version: '3.8'
services:
  postgres-test:
    image: postgres:15
    environment:
      POSTGRES_DB: blockticket_test
      POSTGRES_USER: test
      POSTGRES_PASSWORD: test
  
  redis-test:
    image: redis:7
    
  rabbitmq-test:
    image: rabbitmq:3-management
```

#### Performance Testing Requirements
- **Load Testing**: 10,000 concurrent users during ticket sales
- **Stress Testing**: Service degradation under extreme load
- **Volume Testing**: Large event catalogs (100k+ events)
- **Endurance Testing**: Long-running operations and memory leaks

## Performance Requirements

### Service-Level Performance Targets

#### API Gateway (Port 5000)
- **Route Resolution**: < 5ms (99th percentile)
- **Authentication**: < 10ms token validation
- **Request Forwarding**: < 2ms overhead per request
- **Throughput**: 100,000 requests/minute across all services

#### Identity Service (Port 5001)
- **Authentication**: < 100ms login response (95th percentile)
- **Token Refresh**: < 50ms (95th percentile)
- **User Registration**: < 200ms (95th percentile)
- **MFA Verification**: < 150ms (95th percentile)

#### Event Service (Port 5002)
- **Event Listing**: < 200ms with pagination (95th percentile)
- **Event Details**: < 100ms single event load (95th percentile)
- **Event Search**: < 300ms complex queries (95th percentile)
- **Seat Map Loading**: < 500ms for complex venues (95th percentile)

#### Ticketing Service (Port 5003)
- **Seat Availability**: < 100ms real-time check (95th percentile)
- **Ticket Reservation**: < 300ms seat lock (95th percentile)
- **Purchase Completion**: < 1000ms end-to-end (95th percentile)
- **High-Demand Events**: Support 50,000 concurrent reservation attempts

### Scalability Targets

#### Horizontal Scaling
- **Auto-scaling**: Scale from 2 to 20 instances per service
- **Load Balancing**: Round-robin with health checks
- **Database Read Replicas**: Distribute read operations
- **Cache Scaling**: Redis cluster for distributed caching

#### Concurrent User Support
- **Normal Load**: 10,000 simultaneous active users
- **Peak Load**: 50,000 concurrent users during ticket releases
- **Database Connections**: Pool of 50-100 connections per service
- **Message Queue**: Handle 10,000 messages/second peak throughput

#### Resource Optimization
```yaml
# Example Kubernetes resource limits
resources:
  requests:
    memory: "512Mi"
    cpu: "250m"
  limits:
    memory: "1Gi" 
    cpu: "500m"
```

### Performance Monitoring

#### Key Performance Indicators (KPIs)
- **Response Time**: P50, P95, P99 percentiles per endpoint
- **Throughput**: Requests per second per service
- **Error Rate**: < 0.1% for all non-validation errors
- **Cache Hit Rate**: > 90% for frequently accessed data
- **Database Query Time**: < 50ms average query execution
- **Message Processing**: < 100ms average message handling

#### Service Health Metrics
- **CPU Utilization**: < 70% average, < 90% peak
- **Memory Usage**: < 80% of allocated memory
- **Disk I/O**: Monitor database and log file performance
- **Network Latency**: Inter-service communication timing

#### Business Performance Metrics
- **Ticket Purchase Success Rate**: > 99.5%
- **Payment Processing Time**: < 3 seconds end-to-end
- **Search Response Quality**: Relevant results in < 300ms
- **Seat Reservation Accuracy**: 100% (no double bookings)

## Monitoring & Observability

### Distributed Monitoring Architecture

#### Service Mesh Observability
- **OpenTelemetry**: Distributed tracing across all services
- **Correlation IDs**: Track requests through multiple service calls
- **Span Correlation**: Parent-child relationships in service interactions
- **Baggage Context**: Carry user context through the entire request flow

#### Metrics Collection Strategy

**Business Metrics by Service:**
```csharp
// Example metrics definitions
public static class BusinessMetrics
{
    // Identity Service
    public static readonly Counter UserRegistrations = 
        Metrics.CreateCounter("user_registrations_total", "Total user registrations");
    public static readonly Counter FailedLogins = 
        Metrics.CreateCounter("failed_logins_total", "Failed login attempts");
    
    // Event Service  
    public static readonly Counter EventsCreated = 
        Metrics.CreateCounter("events_created_total", "Events created");
    public static readonly Histogram SearchLatency = 
        Metrics.CreateHistogram("event_search_duration_seconds", "Event search latency");
    
    // Ticketing Service
    public static readonly Counter TicketsSold = 
        Metrics.CreateCounter("tickets_sold_total", "Tickets sold");
    public static readonly Gauge AvailableInventory = 
        Metrics.CreateGauge("available_inventory", "Available ticket inventory");
}
```

**Technical Metrics:**
- **Response Times**: P50, P95, P99 percentiles per endpoint per service
- **Error Rates**: 4xx and 5xx errors by service and endpoint
- **Throughput**: Requests per second per service
- **Resource Usage**: CPU, memory, disk per service instance
- **Database Performance**: Query execution time, connection pool usage
- **Cache Performance**: Hit rates, eviction rates, latency
- **Message Queue**: Message throughput, processing time, dead letter queue size

#### Infrastructure Monitoring

**Prometheus Configuration:**
```yaml
# prometheus.yml excerpt for service discovery
scrape_configs:
  - job_name: 'identity-service'
    kubernetes_sd_configs:
      - role: pod
    relabel_configs:
      - source_labels: [__meta_kubernetes_pod_label_app]
        target_label: service
      
  - job_name: 'event-service'
    kubernetes_sd_configs:
      - role: pod
    relabel_configs:
      - source_labels: [__meta_kubernetes_pod_label_app]
        target_label: service
```

**Grafana Dashboards:**
- **Service Overview**: High-level health across all services
- **Service Detail**: Deep dive into individual service performance
- **Infrastructure**: Kubernetes cluster and node health
- **Business KPIs**: Revenue, user growth, event metrics
- **Alert Status**: Current alerts and their resolution status

### Logging Standards

#### Structured Logging Format
```json
{
  "timestamp": "2025-08-18T10:30:00.123Z",
  "level": "INFO",
  "service": "event-service",
  "version": "1.2.3",
  "environment": "production",
  "message": "Event created successfully",
  "correlationId": "req-abc123-def456",
  "userId": "user-789",
  "organizationId": "org-101",
  "eventId": "event-456",
  "operation": "CreateEvent",
  "duration": 45.2,
  "statusCode": 201,
  "requestPath": "/api/v1/events",
  "requestMethod": "POST"
}
```

#### Log Aggregation with Serilog
```csharp
// Example service configuration
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.WithProperty("Service", "event-service")
    .Enrich.WithProperty("Version", Assembly.GetExecutingAssembly().GetName().Version)
    .Enrich.WithCorrelationId()
    .WriteTo.Console(formatter: new JsonFormatter())
    .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://elasticsearch:9200"))
    {
        IndexFormat = "blockticket-logs-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true
    })
    .CreateLogger();
```

### Alerting Framework

#### Critical Alert Rules
```yaml
# Prometheus alerting rules
groups:
  - name: service-health
    rules:
      - alert: ServiceDown
        expr: up{job=~".*-service"} == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Service {{ $labels.job }} is down"
          
      - alert: HighErrorRate
        expr: rate(http_requests_total{status=~"5.."}[5m]) / rate(http_requests_total[5m]) > 0.01
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High error rate on {{ $labels.service }}"
          
      - alert: HighResponseTime
        expr: histogram_quantile(0.95, rate(http_request_duration_seconds_bucket[5m])) > 0.5
        for: 10m
        labels:
          severity: warning
        annotations:
          summary: "High response time on {{ $labels.service }}"
```

#### Business Alert Rules
- **Payment Failures**: > 5% payment failure rate
- **Ticket Overselling**: Any inventory going negative
- **Authentication Issues**: > 10% login failure rate
- **Revenue Impact**: Significant drop in ticket sales
- **User Experience**: High cart abandonment rates

#### Alert Delivery Channels
- **PagerDuty**: Critical production issues (24/7 on-call)
- **Slack**: Warning-level alerts to development team
- **Email**: Daily/weekly summary reports
- **SMS**: Critical revenue-impacting alerts
- **Dashboard**: Real-time alert status visualization

## Deployment Guidelines

### Microservices Deployment Architecture

#### Container Strategy
Each service is containerized with multi-stage Docker builds:
```dockerfile
# Example Dockerfile for Event Service
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["Services/Event/Event.API/Event.API.csproj", "Services/Event/Event.API/"]
COPY ["Services/Event/Event.Application/Event.Application.csproj", "Services/Event/Event.Application/"]
COPY ["Services/Event/Event.Domain/Event.Domain.csproj", "Services/Event/Event.Domain/"]
COPY ["Services/Event/Event.Infrastructure/Event.Infrastructure.csproj", "Services/Event/Event.Infrastructure/"]
RUN dotnet restore "Services/Event/Event.API/Event.API.csproj"
COPY . .
RUN dotnet build "Services/Event/Event.API/Event.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Event/Event.API/Event.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Event.API.dll"]
```

#### Environment Configuration Strategy
```yaml
# Environment-specific configurations
Development:
  - Docker Compose for local development
  - Shared databases for rapid iteration
  - Hot reload and debugging support
  - Minimal security for ease of development

Staging:
  - Kubernetes deployment with production-like setup
  - Separate databases per service
  - Full authentication and authorization
  - Performance and security testing

Production:
  - Kubernetes with high availability
  - Database clustering and read replicas
  - Full monitoring and alerting
  - Blue-green deployment strategy
```

### Kubernetes Orchestration

#### Service Deployment Manifests
```yaml
# Example: event-service-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: event-service
  labels:
    app: event-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: event-service
  template:
    metadata:
      labels:
        app: event-service
    spec:
      containers:
      - name: event-service
        image: blockticket/event-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__DefaultConnection
          valueFrom:
            secretKeyRef:
              name: event-db-secret
              key: connection-string
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health/live
            port: 80
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health/ready
            port: 80
          initialDelaySeconds: 5
          periodSeconds: 5
```

#### Service Mesh Integration
- **Istio**: Service mesh for traffic management and security
- **Circuit Breakers**: Automatic failure handling between services
- **Canary Deployments**: Gradual rollout of new versions
- **A/B Testing**: Traffic splitting for feature validation

### Database Migration Strategy

#### Per-Service Migration Approach
```csharp
// Example migration in Event Service
public partial class AddApprovalWorkflows : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "approval_workflows",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_approval_workflows", x => x.id);
                table.ForeignKey(
                    name: "fk_approval_workflows_events_event_id",
                    column: x => x.event_id,
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

#### Migration Coordination
- **Independent Migrations**: Each service manages its own database schema
- **Backward Compatibility**: Ensure new versions can work with old schema
- **Migration Testing**: Validate migrations against production data copies
- **Rollback Procedures**: Automated rollback for failed migrations

### Continuous Deployment Pipeline

#### GitOps Workflow
```yaml
# .github/workflows/deploy.yml
name: Deploy to Production
on:
  push:
    branches: [main]
    
jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Run tests
        run: dotnet test --logger trx --results-directory "TestResults"
      
  build-and-deploy:
    needs: test
    runs-on: ubuntu-latest
    strategy:
      matrix:
        service: [identity, event, ticketing, payment, notification]
    steps:
      - name: Build and push Docker image
        run: |
          docker build -f Services/${{ matrix.service }}/Dockerfile -t blockticket/${{ matrix.service }}:${{ github.sha }} .
          docker push blockticket/${{ matrix.service }}:${{ github.sha }}
      
      - name: Deploy to Kubernetes
        run: |
          kubectl set image deployment/${{ matrix.service }}-service ${{ matrix.service }}-service=blockticket/${{ matrix.service }}:${{ github.sha }}
          kubectl rollout status deployment/${{ matrix.service }}-service
```

#### Deployment Safety Measures
- **Health Checks**: Comprehensive health endpoints for each service
- **Graceful Shutdown**: Proper connection draining and cleanup
- **Circuit Breakers**: Automatic traffic diversion during deployments
- **Monitoring Integration**: Automatic rollback on metric threshold breaches
- **Feature Flags**: Runtime feature toggling without redeployment

## Development Workflow

### Microservices Development Strategy

#### Branch Strategy for Multi-Service Repository
- **main**: Production-ready code for all services
- **develop**: Integration branch for cross-service features
- **service/feature/***: Service-specific feature development
- **service/hotfix/***: Critical fixes for individual services
- **release/***: Release preparation with version coordination

#### Service Development Isolation
```bash
# Example: Working on Event Service only
git checkout -b service/event/add-approval-workflows

# Run only Event Service dependencies for development
docker-compose -f docker-compose.dev.yml up postgres redis rabbitmq
dotnet run --project src/Services/Event/Event.API

# Test only Event Service
dotnet test src/Services/Event/Event.Domain.Tests
dotnet test src/Services/Event/Event.Application.Tests
```

#### Cross-Service Development
```bash
# Example: Feature requiring multiple services
git checkout -b feature/ticket-verification-flow

# Start all required services
docker-compose -f docker-compose.dev.yml up

# Develop across Identity, Event, and Ticketing services
# Test integration scenarios
dotnet test tests/Integration.Tests
```

### Code Quality Gates

#### Service-Level Quality Requirements
1. **Unit Tests**: > 80% coverage per service
2. **Integration Tests**: All endpoints tested with real database
3. **Contract Tests**: API contracts validated between services
4. **Security Scan**: No high-severity vulnerabilities
5. **Performance Tests**: Response time regression checks
6. **Code Review**: Minimum 2 approvals for cross-service changes

#### Automated Quality Checks
```yaml
# .github/workflows/quality-gates.yml
name: Quality Gates
on: [pull_request]

jobs:
  unit-tests:
    strategy:
      matrix:
        service: [Identity, Event, Ticketing, Payment, Notification]
    steps:
      - name: Run unit tests
        run: dotnet test src/Services/${{ matrix.service }}/**/*Tests.csproj --logger trx --collect:"XPlat Code Coverage"
      
      - name: Check coverage
        run: |
          coverage=$(grep -o 'line-rate="[^"]*"' coverage.xml | head -1 | cut -d'"' -f2)
          if (( $(echo "$coverage < 0.8" | bc -l) )); then
            echo "Coverage $coverage is below 80% threshold"
            exit 1
          fi

  integration-tests:
    steps:
      - name: Start test infrastructure
        run: docker-compose -f docker-compose.test.yml up -d
      
      - name: Run integration tests
        run: dotnet test tests/Integration.Tests --logger trx
      
      - name: Run contract tests
        run: dotnet test tests/Contract.Tests --logger trx

  security-scan:
    steps:
      - name: Security scan
        run: |
          dotnet list package --vulnerable --include-transitive
          dotnet audit
```

### Release Process

#### Multi-Service Release Coordination
```bash
# 1. Feature freeze and integration testing
git checkout develop
git merge feature/ticket-verification-flow

# 2. Create release branch for version coordination
git checkout -b release/v2.1.0

# 3. Update service versions consistently
# Update version in all affected services:
# - Services/Identity/Identity.API/Identity.API.csproj
# - Services/Event/Event.API/Event.API.csproj  
# - Services/Ticketing/Ticketing.API/Ticketing.API.csproj

# 4. Build and test release candidate
docker-compose -f docker-compose.staging.yml build
docker-compose -f docker-compose.staging.yml up -d

# 5. Run full test suite against staging
dotnet test --configuration Release
k6 run tests/performance/load-test.js

# 6. Deploy to production with rolling updates
kubectl apply -f k8s/
kubectl rollout status deployment/identity-service
kubectl rollout status deployment/event-service
kubectl rollout status deployment/ticketing-service
```

#### Service Versioning Strategy
- **Semantic Versioning**: Major.Minor.Patch for each service
- **API Versioning**: URL-based versioning for breaking changes
- **Database Versioning**: Migration version tracking per service
- **Docker Tags**: Git SHA + semantic version tags

#### Deployment Orchestration
1. **Infrastructure Services**: Databases, message queues, cache
2. **Core Services**: Identity service (authentication dependency)
3. **Business Services**: Event, Payment, Notification services  
4. **User-Facing Services**: Ticketing, API Gateway
5. **Support Services**: Verification, Resale services

### Local Development Environment

#### Docker Compose Development Stack
```yaml
# docker-compose.dev.yml
version: '3.8'
services:
  # Infrastructure
  postgres:
    image: postgres:15
    environment:
      POSTGRES_MULTIPLE_DATABASES: identity_db,event_db,ticketing_db,payment_db,notification_db
      POSTGRES_USER: admin
      POSTGRES_PASSWORD: admin123
    ports:
      - "5432:5432"
  
  redis:
    image: redis:7
    ports:
      - "6379:6379"
  
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
  
  # Services (optional - can run individually in IDE)
  identity-service:
    build:
      context: .
      dockerfile: src/Services/Identity/Dockerfile
    ports:
      - "5001:80"
    depends_on:
      - postgres
      - redis
  
  event-service:
    build:
      context: .
      dockerfile: src/Services/Event/Dockerfile
    ports:
      - "5002:80"
    depends_on:
      - postgres
      - redis
      - rabbitmq
```

#### IDE Configuration
- **Visual Studio**: Solution file includes all services
- **VS Code**: Workspace with service-specific launch configurations
- **JetBrains Rider**: Multi-project solution with debugging support
- **Development Scripts**: Start/stop individual services or full stack

## Communication Patterns

### Inter-Service Communication Architecture

#### Message Bus Integration (MassTransit + RabbitMQ)
All services use MassTransit for reliable message exchange:

```csharp
// Example: Event Service publishing integration event
public class EventCreatedDomainEventHandler : INotificationHandler<EventCreatedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    
    public async Task Handle(EventCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var integrationEvent = new EventCreatedIntegrationEvent
        {
            EventId = domainEvent.Event.Id,
            Title = domainEvent.Event.Title,
            OrganizationId = domainEvent.Event.OrganizationId,
            EventDate = domainEvent.Event.DateTime,
            TicketTypes = domainEvent.Event.TicketTypes.Select(tt => new TicketTypeDto
            {
                Id = tt.Id,
                Name = tt.Name,
                Price = tt.Price.Amount,
                Currency = tt.Price.Currency,
                MaxQuantityPerOrder = tt.MaxQuantityPerOrder
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
        
        await _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
```

#### Service Communication Patterns

**1. Request-Response (Synchronous)**
- **API Gateway ↔ Services**: Direct HTTP calls for real-time operations
- **Service ↔ Identity**: Authentication and authorization validation
- **Ticketing ↔ Event**: Seat availability and pricing queries

**2. Event-Driven (Asynchronous)**
- **Event → Ticketing**: Event created/updated notifications
- **Ticketing → Payment**: Payment processing workflows
- **Payment → Notification**: Payment confirmation events
- **All Services → Audit**: Security and business event logging

**3. Saga Pattern for Distributed Transactions**
```csharp
// Example: Ticket Purchase Saga
public class TicketPurchaseSaga : MassTransitStateMachine<TicketPurchaseState>
{
    public TicketPurchaseSaga()
    {
        Initially(
            When(PurchaseInitiated)
                .Then(ctx => ctx.Instance.CorrelationId = ctx.Data.CorrelationId)
                .Then(ctx => ctx.Instance.UserId = ctx.Data.UserId)
                .Then(ctx => ctx.Instance.EventId = ctx.Data.EventId)
                .PublishAsync(ctx => ctx.Init<ReserveSeat>(new { /* ... */ }))
                .TransitionTo(ReservingSeat));
        
        During(ReservingSeat,
            When(SeatReserved)
                .PublishAsync(ctx => ctx.Init<ProcessPayment>(new { /* ... */ }))
                .TransitionTo(ProcessingPayment),
            When(SeatReservationFailed)
                .PublishAsync(ctx => ctx.Init<NotifyPurchaseFailed>(new { /* ... */ }))
                .TransitionTo(Failed));
                
        During(ProcessingPayment,
            When(PaymentProcessed)
                .PublishAsync(ctx => ctx.Init<ConfirmTicket>(new { /* ... */ }))
                .TransitionTo(Completed),
            When(PaymentFailed)
                .PublishAsync(ctx => ctx.Init<ReleaseSeat>(new { /* ... */ }))
                .TransitionTo(Failed));
    }
}
```

### Message Contracts

#### Integration Event Definitions
```csharp
// Shared contracts in Shared.Contracts assembly
namespace Shared.Contracts.Events
{
    public record EventCreatedIntegrationEvent
    {
        public Guid EventId { get; init; }
        public string Title { get; init; } = string.Empty;
        public Guid OrganizationId { get; init; }
        public DateTime EventDate { get; init; }
        public List<TicketTypeDto> TicketTypes { get; init; } = new();
        public DateTime CreatedAt { get; init; }
    }
    
    public record TicketPurchasedIntegrationEvent
    {
        public Guid TicketId { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public string SeatNumber { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Currency { get; init; } = "USD";
        public DateTime PurchasedAt { get; init; }
    }
    
    public record PaymentProcessedIntegrationEvent
    {
        public Guid PaymentId { get; init; }
        public Guid UserId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "USD";
        public string PaymentMethod { get; init; } = string.Empty;
        public PaymentStatus Status { get; init; }
        public DateTime ProcessedAt { get; init; }
    }
}
```

#### Command Definitions
```csharp
namespace Shared.Contracts.Commands
{
    public record ReserveSeatCommand
    {
        public Guid CorrelationId { get; init; }
        public Guid EventId { get; init; }
        public Guid UserId { get; init; }
        public string SeatNumber { get; init; } = string.Empty;
        public TimeSpan ReservationTimeout { get; init; } = TimeSpan.FromMinutes(15);
    }
    
    public record ProcessPaymentCommand
    {
        public Guid CorrelationId { get; init; }
        public Guid UserId { get; init; }
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "USD";
        public string PaymentMethodId { get; init; } = string.Empty;
        public Dictionary<string, object> Metadata { get; init; } = new();
    }
}
```

### Error Handling & Resilience

#### Retry Policies
```csharp
// MassTransit retry configuration
services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(connectionString);
        
        cfg.UseMessageRetry(r =>
        {
            r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5));
            r.Ignore<ValidationException>();
            r.Ignore<NotFoundException>();
        });
        
        cfg.ConfigureEndpoints(context);
    });
});
```

#### Circuit Breaker Pattern
```csharp
// Example: Circuit breaker for external service calls
public class PaymentServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ICircuitBreaker _circuitBreaker;
    
    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync("/api/payments", request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PaymentResult>();
        });
    }
}
```

#### Dead Letter Queue Handling
```csharp
// Configure dead letter queue for failed messages
cfg.ReceiveEndpoint("event-service-dlq", e =>
{
    e.ConfigureConsumer<DeadLetterQueueConsumer>(context);
    e.Bind("event-service-errors");
});

public class DeadLetterQueueConsumer : IConsumer<ReceiveFault>
{
    public async Task Consume(ConsumeContext<ReceiveFault> context)
    {
        // Log failed message for manual intervention
        // Notify operations team
        // Store in database for later retry
    }
}
```

#### Distributed Tracing
- **Correlation IDs**: Propagated through all service calls
- **OpenTelemetry**: Automatic span creation for HTTP and message operations
- **Custom Tracing**: Business operation tracking across service boundaries
- **Baggage Context**: User and tenant information carried through request flow

This comprehensive microservices architecture ensures scalable, maintainable, and resilient event ticketing capabilities while following industry best practices for distributed systems and clean architecture principles.
