#!/bin/bash

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
REGISTRY="${REGISTRY:-blockticket}"
TAG="${TAG:-$(git -C "$PROJECT_ROOT" rev-parse --short HEAD 2>/dev/null || echo local)}"

IMAGES=(
  "identity-api:src/Services/Identity/Identity.API/Dockerfile"
  "api-gateway:src/ApiGateway/Dockerfile"
  "event-api:src/Services/Event/Event.API/Dockerfile"
  "ticketing-api:src/Services/Ticketing/Dockerfile"
  "notification-api:src/Services/Notification/Dockerfile"
  "blockchain-orchestrator:src/Services/BlockchainOrchestrator/Dockerfile"
  "resale-api:src/Services/Resale/Dockerfile"
  "verification-api:src/Services/Verification/Dockerfile"
)

print_status() {
  echo -e "${BLUE}[INFO]${NC} $1"
}

print_success() {
  echo -e "${GREEN}[SUCCESS]${NC} $1"
}

print_warning() {
  echo -e "${YELLOW}[WARNING]${NC} $1"
}

print_error() {
  echo -e "${RED}[ERROR]${NC} $1"
}

check_docker() {
  if ! command -v docker >/dev/null 2>&1; then
    print_error "Docker is not installed or not in PATH"
    exit 1
  fi
}

build_image() {
  local image_name="$1"
  local dockerfile="$2"

  if [ ! -f "$PROJECT_ROOT/$dockerfile" ]; then
    print_error "Missing Dockerfile: $dockerfile"
    exit 1
  fi

  print_status "Building $REGISTRY/$image_name:$TAG"
  docker build -f "$PROJECT_ROOT/$dockerfile" -t "$REGISTRY/$image_name:$TAG" "$PROJECT_ROOT"
}

push_images() {
  if [ "${PUSH_IMAGES:-false}" != "true" ]; then
    print_warning "Skipping image push. Set PUSH_IMAGES=true to push images."
    return
  fi

  for image in "${IMAGES[@]}"; do
    local image_name="${image%%:*}"
    print_status "Pushing $REGISTRY/$image_name:$TAG"
    docker push "$REGISTRY/$image_name:$TAG"
  done
}

main() {
  print_status "Building BlockTicket images"
  echo "Registry: $REGISTRY"
  echo "Tag: $TAG"
  echo "Project root: $PROJECT_ROOT"

  check_docker

  for image in "${IMAGES[@]}"; do
    build_image "${image%%:*}" "${image#*:}"
  done

  push_images
  print_success "Image build completed"
}

main "$@"
