#!/bin/bash

set -euo pipefail

REGISTRY="${REGISTRY:-blockticket}"
TAG="${TAG:-$(git rev-parse --short HEAD 2>/dev/null || echo local)}"
IMAGE_NAME="$REGISTRY/identity-api:$TAG"
NAMESPACE="${NAMESPACE:-blockticket}"

GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

print_status() { echo -e "${BLUE}[INFO]${NC} $1"; }
print_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }

cd "$(dirname "$0")/../.."

print_status "Building Docker image: $IMAGE_NAME"
docker build -t "$IMAGE_NAME" -f src/Services/Identity/Identity.API/Dockerfile .

if [ "${PUSH_IMAGE:-true}" = "true" ]; then
  print_status "Pushing image: $IMAGE_NAME"
  docker push "$IMAGE_NAME"
fi

print_status "Updating Kubernetes deployment image"
kubectl set image deployment/identity-service identity-api="$IMAGE_NAME" -n "$NAMESPACE"

print_status "Waiting for rollout"
kubectl rollout status deployment/identity-service -n "$NAMESPACE"

print_success "Identity Service updated to $IMAGE_NAME"
