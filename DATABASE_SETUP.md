# Block Ticket Database Setup Guide

## Overview
This guide covers the successful setup and configuration of the Block Ticket microservices backend database using Entity Framework with PostgreSQL.

## ✅ What's Been Completed

### 1. Framework Migration to .NET 9
- **All projects upgraded** from .NET 8 to .NET 9
- **Package versions updated** to .NET 9 compatible versions
- **Entity Framework tools updated** to version 9.0.8
- **Build successful** with no compilation errors

### 2. Database Configuration
- **PostgreSQL connection string** configured for Identity service
- **Database name**: `BlockTicket_Identity`
- **Initial migration created**: `20250805171526_InitialCreate`
- **Database schema applied** successfully

### 3. Project Structure Updated
```
src/
├── Services/
│   ├── Identity/              # ✅ Database configured and running
│   ├── Event/                 # ✅ Updated to .NET 9
│   ├── Ticketing/             # ✅ Updated to .NET 9
│   └── BlockchainOrchestrator/ # ✅ Updated to .NET 9
├── ApiGateway/                # ✅ Updated to .NET 9
└── Shared/
    ├── Common/                # ✅ Updated to .NET 9
    └── Contracts/             # ✅ Updated to .NET 9
```

## Database Connection Details

### Identity Service Database
- **Database**: `BlockTicket_Identity`
- **Host**: `localhost`
- **Username**: `postgres`
- **Password**: `postgres`
- **Connection String**: `Host=localhost;Database=BlockTicket_Identity;Username=postgres;Password=postgres`

## Applied Migrations
- `20250805171526_InitialCreate` - Initial ASP.NET Identity schema with ApplicationUser

## Configuration Files Updated

### Identity Service (`src/Services/Identity/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=BlockTicket_Identity;Username=postgres;Password=postgres",
    "RabbitMQ": "amqp://guest:guest@localhost:5672/"
  },
  "JWT": {
    "SecretKey": "your-super-secret-jwt-key-here-must-be-at-least-256-bits",
    "Issuer": "BlockTicket.Identity",
    "Audience": "BlockTicket.Api",
    "ExpirationMinutes": 60
  }
}
```

### BlockchainOrchestrator Service (`src/Services/BlockchainOrchestrator/appsettings.json`)
```json
{
  "Blockchain": {
    "RpcUrl": "http://localhost:8545",
    "ContractAddress": "0x1234567890123456789012345678901234567890",
    "PrivateKey": "0xprivate_key_here"
  }
}
```

## Entity Framework Commands Used

```bash
# Create initial migration
dotnet ef migrations add InitialCreate

# Apply migration to database
dotnet ef database update

# List applied migrations
dotnet ef migrations list

# Verify with verbose output
dotnet ef database update --verbose
```

## Database Schema
The Identity database includes:
- **ASP.NET Identity tables** (Users, Roles, Claims, etc.)
- **ApplicationUser** with custom `UserType` enum property
- **OpenIddict tables** for OAuth 2.0/OIDC support

## Next Steps for Additional Services

### For Event Service:
```bash
cd src/Services/Event
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### For Ticketing Service:
```bash
cd src/Services/Ticketing
dotnet ef migrations add InitialCreate
dotnet ef database update
```

## Build Verification
✅ **All projects build successfully**
✅ **No compilation errors**
✅ **Database migrations applied**
✅ **Entity Framework working correctly**

## Troubleshooting Notes
- If you encounter .NET version issues, ensure you have .NET 9 runtime installed
- The BlockchainOrchestrator service has a minor warning about Nethereum package dependencies, but it's non-blocking
- Make sure PostgreSQL is running on localhost:5432 before running migrations

## Success Confirmation
```
Build succeeded with 2 warning(s) in 6.1s
Migration applied successfully: 20250805171526_InitialCreate
Database update completed: Done.
```

Your Block Ticket backend is now ready with a properly configured database layer!
