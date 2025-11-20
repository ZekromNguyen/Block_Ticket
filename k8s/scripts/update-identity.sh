#!/bin/bash

# Script to update Identity Service
# 1. Builds Docker image
# 2. Pushes to Docker Hub
# 3. Restarts Kubernetes Deployment

set -e

IMAGE_NAME="ndtuyen0604/identity-api:latest"
SERVICE_DIR="../src/Services/Identity/Identity.API"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }

# 1. Build Docker Image
print_status "Building Docker image: $IMAGE_NAME..."
# Navigate to root to build with correct context
cd ../..
docker build -t $IMAGE_NAME -f src/Services/Identity/Identity.API/Dockerfile .

# 2. Push to Docker Hub
print_status "Pushing image to Docker Hub..."
docker push $IMAGE_NAME

# 3. Restart Deployment
print_status "Restarting Kubernetes deployment..."
kubectl rollout restart deployment/identity-service -n blockticket

# 4. Wait for Rollout
print_status "Waiting for rollout to complete..."
kubectl rollout status deployment/identity-service -n blockticket

print_success "Identity Service updated successfully!"
