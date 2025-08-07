#!/bin/bash

# Block Ticket Backend - Quick Start Script

echo "🚀 Starting Block Ticket Backend Infrastructure..."

# Start infrastructure containers
echo "📦 Starting infrastructure containers..."
cd docker
docker-compose -f docker-compose.infrastructure.yml up -d

echo "⏳ Waiting for services to be ready..."
sleep 30

echo "✅ Infrastructure started successfully!"
echo ""
echo "📊 Available Services:"
echo "- PostgreSQL (Identity): localhost:5432"
echo "- PostgreSQL (Event): localhost:5433" 
echo "- PostgreSQL (Ticketing): localhost:5434"
echo "- Redis: localhost:6379"
echo "- RabbitMQ: localhost:5672 (Management: http://localhost:15672)"
echo "- Ganache Blockchain: localhost:8545"
echo "- Prometheus: http://localhost:9090"
echo "- Grafana: http://localhost:3000 (admin/admin)"
echo ""
echo "🔧 Next steps:"
echo "1. Run database migrations: ./scripts/migrate-databases.sh"
echo "2. Start the microservices: ./scripts/start-services.sh"
echo "3. Open API Gateway Swagger: http://localhost:5000/swagger"
echo ""
echo "🛑 To stop all services: ./scripts/stop-all.sh"
