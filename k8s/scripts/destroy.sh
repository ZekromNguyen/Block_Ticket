#!/bin/bash

# BlockTicket Kubernetes Destruction Script
# This script removes the entire BlockTicket application from Kubernetes

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
NAMESPACE="blockticket"

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

# Confirm destruction
confirm_destruction() {
    echo
    print_warning "This will PERMANENTLY DELETE all BlockTicket resources from Kubernetes!"
    print_warning "This includes:"
    echo "  - All applications and services"
    echo "  - All databases and data"
    echo "  - All persistent volumes"
    echo "  - All configurations and secrets"
    echo
    read -p "Are you sure you want to continue? (yes/NO): " confirm
    
    if [[ $confirm != "yes" ]]; then
        print_status "Destruction cancelled"
        exit 0
    fi
    
    echo
    read -p "Type 'DESTROY' to confirm: " final_confirm
    
    if [[ $final_confirm != "DESTROY" ]]; then
        print_status "Destruction cancelled"
        exit 0
    fi
}

# Delete all resources
delete_resources() {
    print_status "Deleting all BlockTicket resources..."
    
    # Delete ingress and scaling
    print_status "Deleting ingress and scaling configurations..."
    kubectl delete -f ../ingress/ingress.yaml --ignore-not-found=true
    kubectl delete -f ../scaling/hpa.yaml --ignore-not-found=true
    
    # Delete monitoring
    print_status "Deleting monitoring components..."
    kubectl delete -f ../monitoring/grafana.yaml --ignore-not-found=true
    kubectl delete -f ../monitoring/prometheus.yaml --ignore-not-found=true
    
    # Delete services
    print_status "Deleting application services..."
    kubectl delete -f ../services/api-gateway.yaml --ignore-not-found=true
    kubectl delete -f ../services/notification-service.yaml --ignore-not-found=true
    kubectl delete -f ../services/payment-service.yaml --ignore-not-found=true
    kubectl delete -f ../services/ticketing-service.yaml --ignore-not-found=true
    kubectl delete -f ../services/event-service.yaml --ignore-not-found=true
    kubectl delete -f ../services/identity-service.yaml --ignore-not-found=true
    
    # Delete infrastructure
    print_status "Deleting infrastructure components..."
    kubectl delete -f ../infrastructure/mailhog.yaml --ignore-not-found=true
    kubectl delete -f ../infrastructure/rabbitmq.yaml --ignore-not-found=true
    kubectl delete -f ../infrastructure/redis.yaml --ignore-not-found=true
    kubectl delete -f ../infrastructure/postgres.yaml --ignore-not-found=true
    
    print_success "All resources deleted"
}

# Delete persistent volumes
delete_volumes() {
    print_status "Deleting persistent volumes..."
    kubectl delete pvc --all -n $NAMESPACE --ignore-not-found=true
    print_success "Persistent volumes deleted"
}

# Delete namespace
delete_namespace() {
    print_status "Deleting namespace..."
    kubectl delete namespace $NAMESPACE --ignore-not-found=true
    print_success "Namespace deleted"
}

# Main destruction function
main() {
    print_status "BlockTicket Kubernetes Destruction Script"
    echo "Namespace: $NAMESPACE"
    
    confirm_destruction
    
    print_status "Starting destruction process..."
    
    delete_resources
    delete_volumes
    delete_namespace
    
    print_success "BlockTicket application has been completely removed from Kubernetes"
}

# Run main function
main "$@"










