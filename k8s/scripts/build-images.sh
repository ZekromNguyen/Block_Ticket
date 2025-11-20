#!/bin/bash

# BlockTicket Docker Images Build Script
# This script builds all Docker images for the BlockTicket application

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
REGISTRY="${REGISTRY:-blockticket}"
TAG="${TAG:-latest}"
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

# Function to print colored output
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

# Check if Docker is available
check_docker() {
    if ! command -v docker &> /dev/null; then
        print_error "Docker is not installed or not in PATH"
        exit 1
    fi
    print_success "Docker is available"
}

# Build Identity Service image
build_identity_service() {
    print_status "Building Identity Service image..."
    cd "$PROJECT_ROOT"
    
    docker build \
        -f src/Services/Identity/Identity.API/Dockerfile \
        -t "$REGISTRY/identity-api:$TAG" \
        .
    
    print_success "Identity Service image built: $REGISTRY/identity-api:$TAG"
}

# Build Event Service image
build_event_service() {
    print_status "Building Event Service image..."
    cd "$PROJECT_ROOT"
    
    # Create Dockerfile for Event Service if it doesn't exist
    if [ ! -f "src/Services/Event/Event.API/Dockerfile" ]; then
        print_status "Creating Dockerfile for Event Service..."
        cat > "src/Services/Event/Event.API/Dockerfile" << 'EOF'
# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/Event/Event.API/Event.API.csproj", "src/Services/Event/Event.API/"]
COPY ["src/Services/Event/Event.Application/Event.Application.csproj", "src/Services/Event/Event.Application/"]
COPY ["src/Services/Event/Event.Domain/Event.Domain.csproj", "src/Services/Event/Event.Domain/"]
COPY ["src/Services/Event/Event.Infrastructure/Event.Infrastructure.csproj", "src/Services/Event/Event.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]

# Restore dependencies
RUN dotnet restore "src/Services/Event/Event.API/Event.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/Event/Event.API"
RUN dotnet build "Event.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Event.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "Event.API.dll"]
EOF
    fi
    
    docker build \
        -f src/Services/Event/Event.API/Dockerfile \
        -t "$REGISTRY/event-api:$TAG" \
        .
    
    print_success "Event Service image built: $REGISTRY/event-api:$TAG"
}

# Build API Gateway image
build_api_gateway() {
    print_status "Building API Gateway image..."
    cd "$PROJECT_ROOT"
    
    # Create Dockerfile for API Gateway if it doesn't exist
    if [ ! -f "src/ApiGateway/Dockerfile" ]; then
        print_status "Creating Dockerfile for API Gateway..."
        cat > "src/ApiGateway/Dockerfile" << 'EOF'
# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/ApiGateway/ApiGateway.csproj", "src/ApiGateway/"]

# Restore dependencies
RUN dotnet restore "src/ApiGateway/ApiGateway.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/ApiGateway"
RUN dotnet build "ApiGateway.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
EOF
    fi
    
    docker build \
        -f src/ApiGateway/Dockerfile \
        -t "$REGISTRY/api-gateway:$TAG" \
        .
    
    print_success "API Gateway image built: $REGISTRY/api-gateway:$TAG"
}

# Build other service images (Ticketing, Payment, Notification)
build_other_services() {
    local services=("Ticketing" "Payment" "Notification")
    
    for service in "${services[@]}"; do
        print_status "Building ${service} Service image..."
        
        service_lower=$(echo "$service" | tr '[:upper:]' '[:lower:]')
        dockerfile_path="src/Services/${service}/${service}.API/Dockerfile"
        
        # Create Dockerfile if it doesn't exist
        if [ ! -f "$dockerfile_path" ]; then
            print_status "Creating Dockerfile for ${service} Service..."
            mkdir -p "src/Services/${service}/${service}.API"
            cat > "$dockerfile_path" << EOF
# Use the official .NET 9 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the official .NET 9 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Services/${service}/${service}.API/${service}.API.csproj", "src/Services/${service}/${service}.API/"]
COPY ["src/Services/${service}/${service}.Application/${service}.Application.csproj", "src/Services/${service}/${service}.Application/"]
COPY ["src/Services/${service}/${service}.Domain/${service}.Domain.csproj", "src/Services/${service}/${service}.Domain/"]
COPY ["src/Services/${service}/${service}.Infrastructure/${service}.Infrastructure.csproj", "src/Services/${service}/${service}.Infrastructure/"]
COPY ["src/Shared/Common/Shared.Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/Contracts/Shared.Contracts.csproj", "src/Shared/Contracts/"]

# Restore dependencies
RUN dotnet restore "src/Services/${service}/${service}.API/${service}.API.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/Services/${service}/${service}.API"
RUN dotnet build "${service}.API.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "${service}.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage
FROM base AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos '' appuser && chown -R appuser /app
USER appuser

# Copy published application
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \\
    CMD curl -f http://localhost:80/health || exit 1

# Set environment variables
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Entry point
ENTRYPOINT ["dotnet", "${service}.API.dll"]
EOF
        fi
        
        docker build \
            -f "$dockerfile_path" \
            -t "$REGISTRY/${service_lower}-api:$TAG" \
            .
        
        print_success "${service} Service image built: $REGISTRY/${service_lower}-api:$TAG"
    done
}

# Push images to registry
push_images() {
    if [ "$PUSH_IMAGES" = "true" ]; then
        print_status "Pushing images to registry..."
        
        docker push "$REGISTRY/identity-api:$TAG"
        docker push "$REGISTRY/event-api:$TAG"
        docker push "$REGISTRY/api-gateway:$TAG"
        docker push "$REGISTRY/ticketing-api:$TAG"
        docker push "$REGISTRY/payment-api:$TAG"
        docker push "$REGISTRY/notification-api:$TAG"
        
        print_success "All images pushed to registry"
    else
        print_warning "Skipping image push (set PUSH_IMAGES=true to enable)"
    fi
}

# List built images
list_images() {
    print_status "Built images:"
    docker images | grep "$REGISTRY" | grep "$TAG"
}

# Main build function
main() {
    print_status "Building BlockTicket Docker images..."
    echo "Registry: $REGISTRY"
    echo "Tag: $TAG"
    echo "Project Root: $PROJECT_ROOT"
    echo
    
    check_docker
    
    build_identity_service
    build_event_service
    build_api_gateway
    build_other_services
    
    push_images
    list_images
    
    print_success "All Docker images built successfully!"
}

# Run main function
main "$@"










