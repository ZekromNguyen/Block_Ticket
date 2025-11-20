#!/bin/bash

# BlockTicket Minimal Deployment Script (Low Resource)
# This script deploys ONLY Identity and Event services with minimal resources.
# Target Hardware: 2 vCPU, 1 GB RAM

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

NAMESPACE="blockticket"
TEMP_DIR="/tmp/blockticket-minimal"

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
print_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Ensure temp directory exists and is clean
rm -rf $TEMP_DIR
mkdir -p $TEMP_DIR

# Function to patch and apply a manifest
# Arguments: $1 = Source File, $2 = Component Name
apply_minimal() {
    local src=$1
    local name=$2
    local dest="$TEMP_DIR/$(basename $src)"

    print_status "Processing $name..."

    # Copy file
    cp $src $dest

    # Patch: Set replicas to 1
    sed -i '' 's/replicas: [0-9]*/replicas: 1/g' $dest

    # Patch: Set memory request to 64Mi
    sed -i '' 's/memory: "[0-9]*Mi"/memory: "64Mi"/g' $dest
    
    # Patch: Set CPU request to 50m (to be safe)
    sed -i '' 's/cpu: "[0-9]*m"/cpu: "50m"/g' $dest

    # Apply
    kubectl apply -f $dest
}

# Main execution
print_status "Starting MINIMAL deployment (Identity + Event only)..."

# 1. Namespace
kubectl apply -f ../namespace.yaml

# 2. Infrastructure
print_status "Deploying Infrastructure..."
apply_minimal "../infrastructure/postgres.yaml" "PostgreSQL"
apply_minimal "../infrastructure/redis.yaml" "Redis"
apply_minimal "../infrastructure/rabbitmq.yaml" "RabbitMQ"

# Wait for Infra
print_status "Waiting for infrastructure..."
kubectl wait --for=condition=ready pod -l app=postgres -n $NAMESPACE --timeout=300s
kubectl wait --for=condition=ready pod -l app=redis -n $NAMESPACE --timeout=300s
kubectl wait --for=condition=ready pod -l app=rabbitmq -n $NAMESPACE --timeout=300s

# 3. Services
print_status "Deploying Services..."
apply_minimal "../services/identity-service.yaml" "Identity Service"
apply_minimal "../services/event-service.yaml" "Event Service"
apply_minimal "../services/api-gateway.yaml" "API Gateway"

# 4. Ingress
print_status "Deploying Ingress..."
kubectl apply -f ../ingress/ingress.yaml

print_success "Minimal deployment completed!"
print_status "Resources have been patched to: Replicas=1, Memory=64Mi, CPU=50m"
