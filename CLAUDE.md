# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Block Ticket is a blockchain-based event ticketing platform built with .NET 9 microservices. Vietnamese is the primary documentation language. The platform handles the complete event lifecycle: user auth, event management, ticket purchasing, blockchain NFT minting, resale, and verification.

**Tech stack:** .NET 9, ASP.NET Core, PostgreSQL, Redis, RabbitMQ + MassTransit, YARP API Gateway, OpenIddict (OAuth2/OIDC), Docker, Kubernetes, Prometheus + Grafana.

## Build & Run Commands

```bash
# Restore and build the entire solution
dotnet restore
dotnet build

# Build a specific service
dotnet build src/Services/Event/Event.API/Event.API.csproj

# Start infrastructure only (PostgreSQL, Redis, RabbitMQ, MailHog)
docker compose -f docker-compose.dev.yml up -d

# Run a specific service (each in its own terminal)
dotnet run --project src/Services/Identity/Identity.API
dotnet run --project src/Services/Event/Event.API
dotnet run --project src/Services/Ticketing/Ticketing.Api.csproj
dotnet run --project src/Services/BlockchainOrchestrator/BlockchainOrchestrator.csproj
dotnet run --project src/Services/Notification/Notification.csproj
dotnet run --project src/Services/Verification/Verification.Api.csproj
dotnet run --project src/ApiGateway

# Apply EF Core migrations (auto-applied on startup in non-Testing env)
dotnet ef migrations add MigrationName -p src/Services/Event/Event.Infrastructure -s src/Services/Event/Event.API

# Run all tests
dotnet test

# Run specific test project
dotnet test src/Services/Event/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj
```

Startup automatically applies pending EF Core migrations (skip in `Testing` environment). OpenIddict seed data runs on startup. Dev/Staging environments additionally seed permissions, roles, and users.

## Solution Structure & Clean Architecture

The solution has **4 implemented projects** (7 total in `.sln`):

```
src/
├── ApiGateway/                   # YARP reverse proxy (port 5000)
├── Services/
│   ├── Identity/                 # Auth & user management (port 5001)
│   │   ├── Identity.API/         # Controllers, middleware, auth handlers
│   │   ├── Identity.Application/ # MediatR handlers, DTOs, validators
│   │   ├── Identity.Domain/      # User entity (aggregate root), value objects, domain events
│   │   └── Identity.Infrastructure/ # EF Core DbContext, repos, OpenIddict
│   ├── Event/                    # Event & venue management (port 5002)
│   │   ├── Event.API/            # Controllers, middleware (rate limiting, ETag, idempotency)
│   │   ├── Event.Application/    # MediatR commands/queries organized as Features/
│   │   ├── Event.Domain/         # EventAggregate, Venue, TicketType, SeatMap, value objects
│   │   ├── Event.Infrastructure/ # EF Core, Redis caching, repository impls
│   │   ├── Event.API.IntegrationTests/  # WebApplicationFactory-based integration tests
│   │   └── Event.Tests.Integration/     # Additional integration tests
│   └── Ticketing/                # Ticket purchasing & reservations (port 5003)
│       ├── Ticketing.Api.csproj  # NOTE: simpler structure — no Clean Architecture layers yet
│       ├── Ticketing.Domain/     # Entities: Reservation, Ticket, ReservationItem, ReservationPayment
│       ├── Data/TicketingDbContext.cs
│       └── Controllers/
└── Shared/
    ├── Common/        # BaseAuditableEntity, Serilog/OTel extensions, MassTransit helpers
    └── Contracts/     # Integration events & commands (MassTransit message contracts)
```

**Clean Architecture dependency flow:** API → Application → Domain. Infrastructure → both Application and Domain. Domain has zero external dependencies.

The Ticketing service is **not yet migrated to Clean Architecture** — its `.csproj` and `Program.cs` live directly under `src/Services/Ticketing/`.

## Key Architectural Patterns

- **CQRS via MediatR** — every operation is a Command or Query with a dedicated handler. Handlers live in `Features/{Entity}/Commands/{Action}/` or `Features/{Entity}/Queries/{Action}/`.
- **Domain events** — entities extend `BaseAuditableEntity` (from Shared.Common) which provides `Id`, `CreatedAt`, `UpdatedAt`, and `AddDomainEvent()`. Domain events are dispatched before persistence.
- **Value objects** — `Money` (decimal + 3-char ISO currency, with operators), `Slug`, `TimeZoneId`, `GeoCoordinates`, `Email`, `WalletAddress`. All are immutable `record` types.
- **Per-service database** — each service owns its own PostgreSQL database (`BlockTicket_Identity`, `BlockTicket_Event`, `BlockTicket_Ticketing`). No cross-service foreign keys.
- **API versioning** — URL-segment-based (`/api/v1/...`) with fallback to headers/query string.
- **Cursor pagination** — implemented in Event Service for large datasets. See `CursorPagination.cs` and `ICursorRepository.cs`.
- **ETag/conditional requests** — `ETagMiddleware` and `IETaggable` interface on domain entities.
- **Idempotency** — `IdempotentAttribute` and `IdempotencyKey` value object for safe retries of mutating operations.
- **Row-level security** — `RowLevelSecurityConfiguration` and `OrganizationAuthorizationFilter` for multi-tenant data isolation per organization.

## Event Service: Notable Middleware & Cross-Cutting Concerns

The Event Service has the richest middleware stack. Order matters (set in `Program.cs`):

1. Security middleware (`UseComprehensiveSecurity`)
2. Row-level security (`UseRowLevelSecurity`)
3. Serilog request logging
4. Global exception handler (`UseGlobalExceptionHandler`)
5. HSTS / HTTPS redirection
6. CORS
7. Standard ASP.NET auth pipeline

Custom middleware: `AdvancedRateLimitMiddleware`, `ETagMiddleware`, `GlobalExceptionHandlerMiddleware`, `PerformanceMonitoringMiddleware`.

Custom attributes: `[RateLimit]`, `[Idempotent]`.

## Inter-Service Communication

- **Synchronous:** HTTP via YARP API Gateway. The gateway validates JWT tokens from Identity Service.
- **Asynchronous:** MassTransit over RabbitMQ for domain → integration event publishing. Shared.Contracts defines the message schemas.
- **Sagas:** MassTransit state machines for distributed transactions (e.g., ticket purchase orchestration).
- **Correlation IDs:** propagated through all service calls for distributed tracing.

## Identity Service Auth Model

- **OpenIddict** is the OAuth2/OpenID Connect server with reference token support.
- Reference token authentication handler (`ReferenceTokenAuthenticationHandler`) validates tokens at the gateway.
- Role-based access control: `RequireAdminRole` (admin, super_admin), `RequirePromoterRole` (promoter, admin, super_admin).
- Built-in rate limiting policies per IP: `AuthPolicy` (10/min), `MfaPolicy` (5/min), `AdminPolicy` (100/min).
- Features: password history enforcement, concurrent session limits, MFA (TOTP), account lockout after 5 failed attempts (30 min), Discord-based security notifications.

## Infrastructure Ports

| Service | Port |
|---------|------|
| API Gateway (YARP) | 5000 |
| Identity Service | 5001 |
| Event Service | 5002 |
| Ticketing Service | 5003 |
| Resale Service | 5004 |
| Verification Service | 5005 |
| PostgreSQL | 5432 |
| Redis | 6379 |
| RabbitMQ | 5672 (mgmt: 15672) |
| MailHog | 1025 (UI: 8025) |
| Prometheus | 9090 |
| Grafana | 3000 |

## Code Conventions (from .editorconfig & .copilot/rules.md)

- 4-space indentation for C#, 2-space for JSON/YAML/JS
- LF line endings, UTF-8, trailing whitespace trimmed
- PascalCase public, camelCase private/internal
- Expression-bodied members where they improve readability
- XML doc comments on public APIs
- Prefer explicit types over `var` when type isn't obvious
- Controllers stay thin — delegate to MediatR handlers
- Use `Result` pattern over throwing exceptions for expected failures
- Test naming: `Given_When_Then` pattern
- Git commits: conventional commits (`feat:`, `fix:`, `docs:`, etc.)

## Remaining Feature Backlog

Use this backlog as the implementation plan for missing or incomplete product features. Treat **P0 / Must Have** as the minimum shippable backend for a blockchain ticketing platform. Keep new work aligned with the existing Event and Identity service patterns: Clean Architecture, CQRS/MediatR, thin controllers, Result-based expected failures, per-service databases, MassTransit integration events, idempotent writes, and integration tests for user-facing workflows.

### Backlog Status Legend

| Status | Meaning |
|--------|---------|
| `[ ]` | Not started |
| `[~]` | Partially implemented or needs hardening |
| `[x]` | Implemented and covered by tests/docs |

### P0 / Must Have

These features are required before the platform can support a real ticket purchase and entry flow.

| Status | Feature | Why it matters | Implementation notes | Acceptance criteria |
|--------|---------|----------------|----------------------|---------------------|
| `[x]` | Ticketing service Clean Architecture migration | Ticketing is currently flatter than Identity/Event and will become hard to extend safely. | Split into `Ticketing.Application`, `Ticketing.Domain`, and `Ticketing.Infrastructure`; controller logic delegates to MediatR commands/queries; repository, payment, lock, and publisher abstractions added. | API project keeps existing `POST /api/tickets/purchase` compatibility and uses the new application layer. |
| `[x]` | Ticket inventory and reservation locking | Prevents overselling during high-demand sales. | Added reservation expiry, idempotency keys, Redis-backed inventory locks with local fallback, and lock release on failed/expired confirmation. | Duplicate idempotency keys return the original reservation; reservations expire after 15 minutes; failed/expired confirmations release locks. |
| `[x]` | End-to-end checkout workflow | Core buyer journey is incomplete without a purchase state machine. | Implemented create reservation, confirm payment, issue tickets, publish `TicketPurchased`, and publish mint commands through MassTransit. | Buyer can reserve and confirm tickets; compatibility purchase endpoint performs both steps. |
| `[x]` | Payment provider abstraction | Real purchase flow needs a payment boundary even if local dev uses a fake provider. | Added `IPaymentProvider`, fake payment intents, fake confirmation, failure path for `PaymentMethod = "fail"`, and persisted payment states. | Checkout runs locally without a real provider and records transaction metadata. |
| `[x]` | Blockchain Orchestrator service | NFT minting is listed in contracts and docs but no service exists in `src/Services`. | Added worker service consuming `MintTicketCommand` and `BurnTicketCommand`; publishes `TicketMinted` or `TicketMintFailed`; persists mint status. | Successful purchase can trigger mint command and mint result event. |
| `[x]` | Ticket ownership sync after mint | Ticketing must know whether a ticket is pending, minted, failed, or burned. | Ticketing consumes `TicketMinted` and `TicketMintFailed`; updates ticket status, token ID, contract address, and transaction hash idempotently. | Ticket API exposes mint status and blockchain fields. |
| `[x]` | Verification service | Venues need a gate-entry API and scan history. | Added `Verification` API on port 5005; records scans and calls Ticketing internal verification endpoint to validate and mark use. | Active minted ticket can be accepted once; invalid, non-active, and duplicate scans are rejected with a reason. |
| `[x]` | Notification service | Users need purchase, mint, refund, and security messages. | Added worker service consuming `TicketPurchased` and `TicketMinted`; persists/logs notification messages. | Purchase and mint events create idempotent notification records. |
| `[x]` | API Gateway route hardening | Gateway routes exist for missing services and need to match final service APIs. | Ticketing purchase/reservation routes preserve downstream paths; Verification route maps to `/api/verification`; health checks are mapped in new services. | Gateway can route to Ticketing and Verification when services are running. |
| `[x]` | Contract versioning and compatibility tests | Multiple services depend on shared events. | Extended `TicketMinted` with optional backward-compatible fields, added `TicketMintFailed`, and added serialization round-trip tests for P0 commands/events. | Contract tests fail on breaking schema changes once run under .NET 9 SDK. |

### P1 / Should Have

These features are important for a production-like MVP but can follow after the P0 purchase and verification path is working.

| Status | Feature | Why it matters | Implementation notes | Acceptance criteria |
|--------|---------|----------------|----------------------|---------------------|
| `[~]` | Resale service | Secondary sales are part of the product vision and gateway config already reserves port 5004. | Added `Resale` API facade on port 5004, gateway route, compose/k8s wiring, and Ticketing-owned resale list/purchase/cancel commands with transfer events. Still needs production escrow/provider settlement and event-side price policy enforcement. | Owner can list an eligible ticket; buyer can purchase resale ticket; original ticket ownership is transferred and auditable. |
| `[~]` | Waiting list workflow | Supports sold-out events and fair reallocation. | Added join/leave/manual-offer APIs, waiting-list persistence, notification events, and restock/release consumers that create time-limited offers. Still needs offer acceptance reservation handoff and expiry sweep. | Sold-out ticket type allows waitlist join; next user receives time-limited offer when inventory returns. |
| `[~]` | Refund and cancellation workflow | Required for operational support and event cancellation. | Added refund commands, fake-provider refund calls, refund/restock/burn publishing, notifications, and event-cancellation consumer. Still needs provider webhook reconciliation and complete event-service cancellation publication using shared contracts. | Event cancellation can trigger refund flow; refunded tickets cannot verify; blockchain burn command is emitted when needed. |
| `[~]` | Seat map availability integration | Event service has rich seat-map logic, but Ticketing must consume it safely. | Define internal/public APIs or events for seat availability; cache read models; lock seats during reservation. | Seat-specific reservation reflects Event seat map rules and cannot double-book seats. |
| `[~]` | Admin/support operations | Operators need safe tools to resolve failed purchases and mints. | Added admin APIs for payment lookup, reservation force-expire, mint retry, verification override, admin refund, and audit notes behind `RequireAdminRole`. Still needs executable authorization integration tests once .NET 9 SDK is available. | Admin actions require admin role, are audited, and are covered by authorization tests. |
| `[~]` | OpenTelemetry tracing across services | Needed to debug distributed purchase flows. | Shared OTel instrumentation is now wired into Ticketing, Resale, Verification, Notification, BlockchainOrchestrator, and Gateway startup paths. Still needs exporter configuration and correlation ID enrichment across RabbitMQ messages. | A checkout request can be followed through gateway, ticketing, RabbitMQ, worker, and notification logs/traces. |
| `[~]` | Docker Compose production parity | Current compose files do not run all documented services. | Added Resale API container to `docker-compose.dev.yml` app profile and kept P0 app services wired. Still missing full production compose parity and local blockchain node. | `docker compose -f docker-compose.dev.yml up` starts required infrastructure; full compose can run implemented app services. |
| `[~]` | Kubernetes manifests for new services | Deployment manifests must match actual services. | Added Resale and Verification deployment/service manifests with probes/config. Still needs secret hardening, HPA tuning, and validation against a local cluster. | K8s manifests apply cleanly in a local cluster after required secrets are provided. |

### P2 / Could Have

These improve scale, operations, and product completeness after the core lifecycle is stable.

| Status | Feature | Why it matters | Implementation notes | Acceptance criteria |
|--------|---------|----------------|----------------------|---------------------|
| `[ ]` | Advanced pricing and promotions in checkout | Event service has pricing/campaign concepts that should affect final ticket price. | Evaluate pricing rules at reservation time; snapshot discounts/taxes/fees into reservation items. | Checkout total matches pricing rules and remains stable after reservation is created. |
| `[ ]` | Analytics/read models | Promoters need sales and attendance visibility. | Build projections from purchase, resale, refund, and verification events. | APIs expose sales count, revenue, attendance, scan failures, and conversion metrics per event. |
| `[ ]` | Public event discovery hardening | Public catalog must be fast and cacheable. | Ensure cursor pagination, cache invalidation, ETags, and published-only filters are consistently applied. | Public search remains fast with large seed data and never exposes draft/private events. |
| `[ ]` | Fraud/risk checks | Ticketing benefits from risk signals before issuing valuable tickets. | Add rule hooks for velocity checks, suspicious account flags, payment mismatches, and wallet risk. | Risky checkout can be challenged, blocked, or manually reviewed with clear audit trail. |
| `[ ]` | Multi-currency settlement | Money value objects exist but payment/checkout should define currency behavior. | Define allowed currencies per event, conversion policy, fees, and provider settlement metadata. | Reservation and payment currency are explicit and cannot mismatch event policy. |
| `[ ]` | Developer seed scenarios | Local dev needs repeatable data for manual testing. | Add seed users, events, ticket types, seat maps, sold-out scenarios, and sample purchases. | A fresh dev environment can exercise purchase, mint, verification, resale, and refund flows. |

### Cross-Cutting Implementation Rules

- Keep service boundaries strict: no cross-service foreign keys and no direct database access across services.
- Prefer asynchronous integration through `Shared.Contracts` for lifecycle changes; use synchronous HTTP only for query/validation boundaries that need immediate answers.
- Make all mutating external callbacks idempotent: payment webhooks, mint results, refund results, and verification scans.
- Add integration tests before or with each workflow that spans controllers, EF Core, and message publishing/consumption.
- Update `README.md`, `docker-compose.dev.yml`, gateway config, and docs when a service becomes runnable.
- Do not add new service patterns unless they solve a real gap; follow Identity/Event conventions first.

### Suggested Implementation Order

1. Migrate Ticketing to Clean Architecture while preserving current API behavior.
2. Implement reservations, inventory locking, and checkout payment abstraction with a fake provider.
3. Publish purchase and mint commands from Ticketing through MassTransit.
4. Add Blockchain Orchestrator worker and consume mint results back into Ticketing.
5. Add Verification service for gate scans.
6. Add Notification worker for purchase/mint/refund/security messages.
7. Add Resale and waiting-list workflows.
8. Harden gateway, compose, Kubernetes, observability, contract tests, and admin operations.
