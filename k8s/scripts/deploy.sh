#!/bin/bash

# BlockTicket Kubernetes Deployment Script
# This script deploys the entire BlockTicket application to Kubernetes

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="blockticket"
ENVIRONMENT="${ENVIRONMENT:-production}"

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

# Check if kubectl is available
check_kubectl() {
    if ! command -v kubectl &> /dev/null; then
        print_error "kubectl is not installed or not in PATH"
        exit 1
    fi
    print_success "kubectl is available"
}

# Check if the cluster is accessible
check_cluster() {
    if ! kubectl cluster-info &> /dev/null; then
        print_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    print_success "Connected to Kubernetes cluster"
}

# Create namespace if it doesn't exist
create_namespace() {
    print_status "Creating namespace..."
    kubectl apply -f ../namespace.yaml
    print_success "Namespace created/updated"
}

# Deploy infrastructure components
deploy_infrastructure() {
    print_status "Deploying infrastructure components..."
    
    # Deploy PostgreSQL
    print_status "Deploying PostgreSQL..."
    kubectl apply -f ../infrastructure/postgres.yaml
    
    # Deploy Redis
    print_status "Deploying Redis..."
    kubectl apply -f ../infrastructure/redis.yaml
    
    # Deploy RabbitMQ
    print_status "Deploying RabbitMQ..."
    kubectl apply -f ../infrastructure/rabbitmq.yaml
    
    # Deploy MailHog
    print_status "Deploying MailHog..."
    kubectl apply -f ../infrastructure/mailhog.yaml
    
    print_success "Infrastructure components deployed"
}

# Wait for infrastructure to be ready
wait_for_infrastructure() {
    print_status "Waiting for infrastructure to be ready..."
    
    # Wait for PostgreSQL
    kubectl wait --for=condition=ready pod -l app=postgres -n $NAMESPACE --timeout=300s
    
    # Wait for Redis
    kubectl wait --for=condition=ready pod -l app=redis -n $NAMESPACE --timeout=300s
    
    # Wait for RabbitMQ
    kubectl wait --for=condition=ready pod -l app=rabbitmq -n $NAMESPACE --timeout=300s
    
    print_success "Infrastructure is ready"
}

# Deploy application services
deploy_services() {
    print_status "Deploying application services..."
    
    # Deploy Identity Service
    print_status "Deploying Identity Service..."
    kubectl apply -f ../services/identity-service.yaml
    
    # Deploy Event Service
    print_status "Deploying Event Service..."
    kubectl apply -f ../services/event-service.yaml
    
    # Deploy Ticketing Service
    print_status "Deploying Ticketing Service..."
    kubectl apply -f ../services/ticketing-service.yaml
    
    # Deploy Payment Service
    print_status "Deploying Payment Service..."
    kubectl apply -f ../services/payment-service.yaml
    
    # Deploy Notification Service
    print_status "Deploying Notification Service..."
    kubectl apply -f ../services/notification-service.yaml
    
    # Deploy API Gateway
    print_status "Deploying API Gateway..."
    kubectl apply -f ../services/api-gateway.yaml
    
    print_success "Application services deployed"
}

# Deploy monitoring
deploy_monitoring() {
    print_status "Deploying monitoring components..."
    
    # Deploy Prometheus
    print_status "Deploying Prometheus..."
    kubectl apply -f ../monitoring/prometheus.yaml
    
    # Deploy Grafana
    print_status "Deploying Grafana..."
    kubectl apply -f ../monitoring/grafana.yaml
    
    print_success "Monitoring components deployed"
}

# Deploy ingress and scaling
deploy_ingress_and_scaling() {
    print_status "Deploying ingress and scaling configurations..."
    
    # Deploy Ingress
    print_status "Deploying Ingress..."
    kubectl apply -f ../ingress/ingress.yaml
    
    # Deploy HPA
    print_status "Deploying Horizontal Pod Autoscalers..."
    kubectl apply -f ../scaling/hpa.yaml
    
    print_success "Ingress and scaling configurations deployed"
}

# Wait for services to be ready
wait_for_services() {
    print_status "Waiting for services to be ready..."
    
    # Wait for Identity Service
    kubectl wait --for=condition=ready pod -l app=identity-service -n $NAMESPACE --timeout=600s
    
    # Wait for Event Service
    kubectl wait --for=condition=ready pod -l app=event-service -n $NAMESPACE --timeout=600s
    
    # Wait for API Gateway
    kubectl wait --for=condition=ready pod -l app=api-gateway -n $NAMESPACE --timeout=300s
    
    print_success "All services are ready"
}

# Display deployment status
show_status() {
    print_status "Deployment Status:"
    echo
    kubectl get all -n $NAMESPACE
    echo
    kubectl get ingress -n $NAMESPACE
    echo
    print_success "Deployment completed successfully!"
    echo
    print_status "You can access your application at:"
    echo "  - API: https://api.blockticket.com"
    echo "  - Monitoring: https://monitoring.blockticket.com"
    echo
    print_warning "Note: Make sure your DNS is configured to point to your ingress controller"
}

# Main deployment function
main() {
    print_status "Starting BlockTicket Kubernetes deployment..."
    echo "Environment: $ENVIRONMENT"
    echo "Namespace: $NAMESPACE"
    echo
    
    check_kubectl
    check_cluster
    create_namespace
    deploy_infrastructure
    wait_for_infrastructure
    deploy_services
    deploy_monitoring
    deploy_ingress_and_scaling
    wait_for_services
    show_status
}

# Run main function
main "$@"










