#!/bin/bash

# Stop all services script

echo "🛑 Stopping Block Ticket Backend services..."

# Stop infrastructure containers
echo "📦 Stopping infrastructure containers..."
cd docker
docker-compose -f docker-compose.infrastructure.yml down

echo "✅ All services stopped successfully!"
echo ""
echo "💾 Data volumes are preserved. To remove them completely:"
echo "docker-compose -f docker-compose.infrastructure.yml down -v"
