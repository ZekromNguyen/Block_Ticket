#!/bin/bash

# BlockTicket Production Setup Script
# Updates configuration with real domain and secure secrets.

set -e

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }

# 1. Prompt for Domain
echo "Enter your domain name (e.g., myapp.com or 1.2.3.4.nip.io):"
read DOMAIN
if [ -z "$DOMAIN" ]; then
    echo "Domain cannot be empty."
    exit 1
fi

# 2. Generate Secrets
print_status "Generating secure secrets..."
POSTGRES_PASSWORD=$(openssl rand -base64 16 | tr -d '\n')
REDIS_PASSWORD=$(openssl rand -base64 16 | tr -d '\n')
RABBITMQ_PASSWORD=$(openssl rand -base64 16 | tr -d '\n')
JWT_SECRET=$(openssl rand -base64 32 | tr -d '\n')
ENCRYPTION_KEY=$(openssl rand -base64 24 | tr -d '\n')

# Base64 Encode Secrets for K8s Secrets (remove newlines)
B64_POSTGRES=$(echo -n "$POSTGRES_PASSWORD" | base64)
B64_REDIS=$(echo -n "$REDIS_PASSWORD" | base64)
B64_RABBITMQ=$(echo -n "$RABBITMQ_PASSWORD" | base64)
B64_JWT=$(echo -n "$JWT_SECRET" | base64)
B64_ENC=$(echo -n "$ENCRYPTION_KEY" | base64)

# 3. Update Infrastructure Secrets
print_status "Updating infrastructure secrets..."

# Postgres
sed -i '' "s/POSTGRES_PASSWORD: .*/POSTGRES_PASSWORD: $B64_POSTGRES/g" ../infrastructure/postgres.yaml

# Redis
sed -i '' "s/REDIS_PASSWORD: .*/REDIS_PASSWORD: $B64_REDIS/g" ../infrastructure/redis.yaml

# RabbitMQ
sed -i '' "s/RABBITMQ_DEFAULT_PASS: .*/RABBITMQ_DEFAULT_PASS: $B64_RABBITMQ/g" ../infrastructure/rabbitmq.yaml

# 4. Update Connection Strings & Secrets in Services
print_status "Updating service configurations..."

# Helper to base64 encode a string
b64() { echo -n "$1" | base64; }

# --- Identity Service ---
CONN_IDENTITY="Host=postgres-service;Database=BlockTicket_Identity;Username=postgres;Password=$POSTGRES_PASSWORD;Port=5432;"
CONN_REDIS="redis-service:6379,password=$REDIS_PASSWORD"

sed -i '' "s/ConnectionStrings__DefaultConnection: .*/ConnectionStrings__DefaultConnection: $(b64 "$CONN_IDENTITY")/g" ../services/identity-service.yaml
sed -i '' "s/ConnectionStrings__Redis: .*/ConnectionStrings__Redis: $(b64 "$CONN_REDIS")/g" ../services/identity-service.yaml
sed -i '' "s/Security__JwtSecretKey: .*/Security__JwtSecretKey: $B64_JWT/g" ../services/identity-service.yaml
sed -i '' "s/JWT__SecretKey: .*/JWT__SecretKey: $B64_JWT/g" ../services/identity-service.yaml
sed -i '' "s/Security__EncryptionKey: .*/Security__EncryptionKey: $B64_ENC/g" ../services/identity-service.yaml
# Update RabbitMQ password in ConfigMap (Identity Service)
sed -i '' "s/RabbitMQ__Password: \"guest\"/RabbitMQ__Password: \"$RABBITMQ_PASSWORD\"/g" ../services/identity-service.yaml

# --- Event Service ---
CONN_EVENT="Host=postgres-service;Port=5432;Database=BlockTicket_Event;Username=postgres;Password=$POSTGRES_PASSWORD;Include Error Detail=true"
CONN_RABBITMQ="amqp://guest:$RABBITMQ_PASSWORD@rabbitmq-service:5672/"

sed -i '' "s/ConnectionStrings__DefaultConnection: .*/ConnectionStrings__DefaultConnection: $(b64 "$CONN_EVENT")/g" ../services/event-service.yaml
sed -i '' "s/ConnectionStrings__Redis: .*/ConnectionStrings__Redis: $(b64 "$CONN_REDIS")/g" ../services/event-service.yaml
sed -i '' "s/ConnectionStrings__RabbitMQ: .*/ConnectionStrings__RabbitMQ: $(b64 "$CONN_RABBITMQ")/g" ../services/event-service.yaml
sed -i '' "s/Jwt__Key: .*/Jwt__Key: $B64_JWT/g" ../services/event-service.yaml
sed -i '' "s/RabbitMQ__Password: .*/RabbitMQ__Password: $B64_RABBITMQ/g" ../services/event-service.yaml
sed -i '' "s/Redis__ConnectionString: .*/Redis__ConnectionString: $(b64 "$CONN_REDIS")/g" ../services/event-service.yaml
sed -i '' "s/Security__JwtSettings__SecretKey: .*/Security__JwtSettings__SecretKey: $B64_JWT/g" ../services/event-service.yaml

# --- Ticketing Service ---
CONN_TICKETING="Host=postgres-service;Port=5432;Database=BlockTicket_Ticketing;Username=postgres;Password=$POSTGRES_PASSWORD;Include Error Detail=true"
sed -i '' "s/ConnectionStrings__DefaultConnection: .*/ConnectionStrings__DefaultConnection: $(b64 "$CONN_TICKETING")/g" ../services/ticketing-service.yaml

# --- Payment Service ---
CONN_PAYMENT="Host=postgres-service;Port=5432;Database=BlockTicket_Payment;Username=postgres;Password=$POSTGRES_PASSWORD;Include Error Detail=true"
sed -i '' "s/ConnectionStrings__DefaultConnection: .*/ConnectionStrings__DefaultConnection: $(b64 "$CONN_PAYMENT")/g" ../services/payment-service.yaml

# --- Notification Service ---
CONN_NOTIFICATION="Host=postgres-service;Port=5432;Database=BlockTicket_Notification;Username=postgres;Password=$POSTGRES_PASSWORD;Include Error Detail=true"
sed -i '' "s/ConnectionStrings__DefaultConnection: .*/ConnectionStrings__DefaultConnection: $(b64 "$CONN_NOTIFICATION")/g" ../services/notification-service.yaml

# 5. Update Ingress
print_status "Updating Ingress hosts..."
sed -i '' "s/host: api.blockticket.com/host: api.$DOMAIN/g" ../ingress/ingress.yaml
sed -i '' "s/host: blockticket.com/host: $DOMAIN/g" ../ingress/ingress.yaml
sed -i '' "s/host: monitoring.blockticket.com/host: monitoring.$DOMAIN/g" ../ingress/ingress.yaml
sed -i '' "s/cors-allow-origin: .*/cors-allow-origin: \"https:\/\/$DOMAIN,https:\/\/app.$DOMAIN\"/g" ../ingress/ingress.yaml

print_success "Configuration updated successfully!"
print_status "Secrets generated:"
echo "Postgres: $POSTGRES_PASSWORD"
echo "Redis: $REDIS_PASSWORD"
echo "RabbitMQ: $RABBITMQ_PASSWORD"
echo "JWT Secret: $JWT_SECRET"
