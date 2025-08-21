# GitHub Copilot Rules for Block Ticket Event Service

## Code Quality & Standards

### C# Coding Standards
- Use meaningful variable and method names that clearly express intent
- Follow PascalCase for public members, camelCase for private/internal members
- Always include XML documentation comments for public APIs
- Use nullable reference types appropriately with proper null checks
- Prefer explicit types over `var` when type isn't obvious from context
- Use expression-bodied members when they improve readability

### Domain-Driven Design (DDD) Patterns
- Keep domain entities pure with business logic encapsulated
- Use value objects for primitive obsession (Money, Slug, TimeZoneId, etc.)
- Domain events should be immutable and represent business events
- Aggregates should maintain consistency boundaries
- Repository interfaces belong in Domain layer, implementations in Infrastructure

### Clean Architecture Layers
- **Domain**: Core business logic, entities, value objects, domain events, interfaces
- **Application**: Use cases, DTOs, validators, handlers (MediatR)
- **Infrastructure**: Data access, external services, repository implementations
- **API**: Controllers, middleware, configuration, dependency injection

### Error Handling
- Use domain exceptions for business rule violations
- Prefer Result pattern over throwing exceptions for expected failures
- Log errors with structured logging using ILogger
- Include correlation IDs for request tracing
- Validate inputs at application boundaries

### Dependency Injection & Services
- Register services with appropriate lifetimes (Scoped for repositories, Singleton for configuration)
- Use interface segregation - small, focused interfaces
- Avoid service locator pattern - use constructor injection
- Configure services in separate extension methods for organization

## Entity Framework & Database

### Repository Pattern
- Keep repositories focused on data access only
- Use async/await for all database operations
- Implement cursor-based pagination for large datasets
- Include proper error handling for database constraints
- Use explicit Loading for navigation properties when needed

### Entity Configuration
- Configure entities using Fluent API in separate configuration classes
- Set up proper indexes for query performance
- Configure value object conversions properly
- Use shadow properties for audit fields when appropriate
- Set up proper cascade delete behaviors

### Migrations
- Include meaningful migration names describing the change
- Add comments explaining complex schema changes
- Test migrations against production-like data volumes
- Include rollback scenarios for breaking changes

## API Design

### Controller Patterns
- Keep controllers thin - delegate to MediatR handlers
- Use proper HTTP status codes (201 for created, 204 for no content, etc.)
- Include proper API documentation with OpenAPI attributes
- Implement consistent error response format
- Use action filters for cross-cutting concerns

### Request/Response Models
- Separate DTOs from domain entities
- Include proper validation attributes on request models
- Use AutoMapper or manual mapping - avoid direct entity exposure
- Include pagination metadata in responses
- Support both synchronous and asynchronous operations

### Security & Authorization
- Validate organization-scoped access for multi-tenant operations
- Include proper authentication attributes on protected endpoints
- Sanitize input data to prevent injection attacks
- Use HTTPS-only cookies for sensitive data
- Implement rate limiting for public endpoints

## Testing Strategy

### Unit Tests
- Test business logic in domain entities thoroughly
- Mock external dependencies using interfaces
- Use descriptive test method names following Given_When_Then pattern
- Test both happy path and error scenarios
- Maintain high code coverage for critical business logic

### Integration Tests
- Test API endpoints with real database (in-memory or test container)
- Verify database constraints and relationships
- Test authentication and authorization flows
- Include performance tests for critical operations
- Test data serialization/deserialization

## Performance & Scalability

### Caching Strategy
- Cache read-heavy data with appropriate TTL
- Use distributed caching for multi-instance deployments
- Implement cache invalidation for data consistency
- Monitor cache hit rates and adjust strategies accordingly
- Consider cache warming for frequently accessed data

### Query Optimization
- Use projection queries (Select) to fetch only needed data
- Implement efficient pagination using cursor-based approach
- Add database indexes for frequently queried columns
- Use compiled queries for repeated operations
- Monitor and log slow queries

### Async Programming
- Use async/await throughout the call stack
- Avoid blocking async calls with .Result or .Wait()
- Use ConfigureAwait(false) in library code
- Implement proper cancellation token support
- Handle timeout scenarios gracefully

## Monitoring & Observability

### Logging
- Use structured logging with semantic properties
- Include correlation IDs for request tracing
- Log at appropriate levels (Debug, Info, Warning, Error)
- Avoid logging sensitive data (PII, credentials)
- Include performance metrics in logs

### Health Checks
- Implement health checks for all external dependencies
- Include database connectivity and performance checks
- Monitor critical business metrics
- Set up alerting for health check failures
- Provide detailed health check responses

## Security Best Practices

### Data Protection
- Encrypt sensitive data at rest and in transit
- Use parameterized queries to prevent SQL injection
- Validate and sanitize all input data
- Implement proper CORS policies
- Use secure headers (HSTS, CSP, etc.)

### Authentication & Authorization
- Use industry-standard authentication protocols (OAuth 2.0, JWT)
- Implement proper session management
- Use role-based access control (RBAC)
- Audit security-sensitive operations
- Implement account lockout policies

## Development Workflow

### Git Practices
- Use conventional commit messages (feat:, fix:, docs:, etc.)
- Keep commits atomic and focused
- Write descriptive commit messages explaining why, not what
- Use feature branches for development
- Squash commits before merging to main

### Code Review Guidelines
- Review for business logic correctness
- Check for proper error handling and edge cases
- Verify security implications of changes
- Ensure adequate test coverage
- Review performance implications

### Documentation
- Keep README files updated with setup instructions
- Document API changes in CHANGELOG
- Include architecture decision records (ADRs) for major decisions
- Maintain up-to-date API documentation
- Document configuration options and environment variables

## Event Service Specific Rules

### Event Management
- Validate event dates are in the future when creating/updating
- Ensure venue capacity constraints are respected
- Implement proper event lifecycle state transitions
- Handle timezone conversions consistently
- Validate ticket type relationships and constraints

### Ticket Management
- Implement proper inventory tracking and allocation
- Handle concurrent reservation scenarios
- Implement reservation TTL and cleanup
- Validate pricing rules and discount calculations
- Ensure seat assignment consistency

### Pricing & Revenue
- Use Money value object for all monetary calculations
- Handle currency conversions consistently
- Implement proper tax calculation logic
- Validate discount and coupon application rules
- Track revenue attribution accurately

### Search & Filtering
- Implement efficient full-text search using PostgreSQL features
- Cache search results for common queries
- Support geo-location based filtering
- Implement faceted search capabilities
- Handle search result ranking and relevance

### Integration Patterns
- Use event-driven architecture for service communication
- Implement proper retry and circuit breaker patterns
- Handle external service failures gracefully
- Implement idempotency for critical operations
- Use correlation IDs for distributed tracing
