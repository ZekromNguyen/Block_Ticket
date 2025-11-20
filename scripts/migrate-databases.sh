#!/bin/bash

set -e

# Database Migration Script

echo "🗄️  Running database migrations..."

# Identity Service Migration
echo "📊 Migrating Identity database..."
dotnet ef database update --project src/Services/Identity/Identity.Infrastructure --startup-project src/Services/Identity/Identity.API
echo "✅ Identity database migrated successfully"

# Event Service Migration
echo "📊 Migrating Event database..."
dotnet ef database update --project src/Services/Event/Event.Infrastructure --startup-project src/Services/Event/Event.API
echo "✅ Event database migrated successfully"

# Ticketing Service Migration
echo "📊 Migrating Ticketing database..."
dotnet ef database update --project src/Services/Ticketing/Ticketing.Api.csproj --startup-project src/Services/Ticketing/Ticketing.Api.csproj
echo "✅ Ticketing database migrated successfully"

echo ""
echo "✅ All database migrations completed!"
echo "🚀 Ready to start the microservices with: ./scripts/start-services.sh"
