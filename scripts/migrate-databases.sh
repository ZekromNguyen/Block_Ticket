#!/bin/bash

# Database Migration Script

echo "🗄️  Running database migrations..."

cd ..

# Identity Service Migration
echo "📊 Migrating Identity database..."
cd src/Services/Identity
dotnet ef database update
if [ $? -eq 0 ]; then
    echo "✅ Identity database migrated successfully"
else
    echo "❌ Identity database migration failed"
fi

# Event Service Migration  
echo "📊 Migrating Event database..."
cd ../Event
dotnet ef database update
if [ $? -eq 0 ]; then
    echo "✅ Event database migrated successfully"
else
    echo "❌ Event database migration failed"
fi

# Ticketing Service Migration
echo "📊 Migrating Ticketing database..."
cd ../Ticketing
dotnet ef database update
if [ $? -eq 0 ]; then
    echo "✅ Ticketing database migrated successfully"
else
    echo "❌ Ticketing database migration failed"
fi

echo ""
echo "✅ All database migrations completed!"
echo "🚀 Ready to start the microservices with: ./scripts/start-services.sh"
