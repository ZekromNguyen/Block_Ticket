# BlockTicket Kubernetes Deployment

This directory contains Kubernetes deployment files and scripts for the BlockTicket microservices application.

## Architecture Overview

The BlockTicket application consists of the following components:

### Core Services
- **Identity Service** - Authentication, authorization, and user management
- **Event Service** - Event creation, management, and seat mapping
- **Ticketing Service** - Ticket booking and management
- **Notification Service** - Email and SMS notifications
- **Resale Service** - Ticket resale and waiting list workflows
- **Verification Service** - Ticket verification workflows
- **API Gateway** - Request routing and load balancing

### Infrastructure
- **PostgreSQL** - Primary database for all services
- **Redis** - Caching and session storage
- **RabbitMQ** - Message queue for inter-service communication
- **MailHog** - Development email testing (replace with real SMTP in production)

### Monitoring
- **Prometheus** - Metrics collection
- **Grafana** - Monitoring dashboards

## Directory Structure

Active deployment scripts deploy API Gateway, Identity, Event, Ticketing, Notification, Resale, and Verification. The old payment placeholder manifest was removed because there is no matching Payment service source project or Dockerfile in this repository.

```
k8s/
├── README.md                     # This file
├── namespace.yaml               # Kubernetes namespaces
├── infrastructure/              # Infrastructure components
│   ├── postgres.yaml           # PostgreSQL database
│   ├── redis.yaml              # Redis cache
│   ├── rabbitmq.yaml           # RabbitMQ message broker
│   └── mailhog.yaml            # MailHog email testing
├── services/                    # Application services
│   ├── identity-service.yaml   # Identity service deployment
│   ├── event-service.yaml      # Event service deployment
│   ├── ticketing-service.yaml  # Ticketing service deployment
│   ├── notification-service.yaml # Notification service deployment
│   └── api-gateway.yaml        # API Gateway deployment
├── monitoring/                  # Monitoring stack
├── ingress/                     # External access
│   └── ingress.yaml            # Nginx ingress controller
├── scaling/                     # Auto-scaling
│   └── hpa.yaml                # Horizontal Pod Autoscalers
└── scripts/                     # Deployment scripts
    ├── deploy.sh               # Full deployment script
    ├── destroy.sh              # Cleanup script
    ├── build-images.sh         # Docker image build script
    └── update-identity.sh      # Immutable-tag identity update helper
```

## Prerequisites

1. **Kubernetes Cluster**: A running Kubernetes cluster (v1.24+)
2. **kubectl**: Kubernetes command-line tool
3. **Docker**: For building container images
4. **Ingress Controller**: Nginx ingress controller installed in your cluster
5. **Storage Class**: A default storage class for persistent volumes

### Optional
- **Cert-Manager**: For automatic SSL certificate management
- **Metrics Server**: For HPA (Horizontal Pod Autoscaler) functionality

## Quick Start

For the stronger portfolio deployment path, use Terraform/OpenTofu in `infra/terraform/aws` and Kustomize overlays in `k8s/overlays`. See `docs/devops/phase-2-platform.md`.

### 1. Build Docker Images

First, build all the Docker images:

```bash
cd k8s/scripts
chmod +x *.sh
./build-images.sh
```

Environment variables:
- `REGISTRY`: Docker registry (default: `blockticket`)
- `TAG`: Image tag (default: current Git short SHA, or `local` outside Git)
- `PUSH_IMAGES`: Set to `true` to push images to registry

Example with custom registry:
```bash
REGISTRY=your-registry.com/blockticket TAG=v1.0.0 PUSH_IMAGES=true ./build-images.sh
```

### 2. Deploy to Kubernetes

Deploy the entire application:

```bash
./deploy.sh
```

This script will:
1. Create the namespace
2. Deploy infrastructure components (PostgreSQL, Redis, RabbitMQ)
3. Apply baseline NetworkPolicies and PodDisruptionBudgets
4. Wait for infrastructure to be ready
5. Deploy application services
6. Deploy monitoring stack
7. Configure ingress and auto-scaling
8. Display deployment status

### 3. Access the Application

After deployment, you can access:

- **API**: `https://api.blockticket.com` (configure DNS)
- **Monitoring**: `https://monitoring.blockticket.com`
  - Grafana: `/grafana` (admin/admin)
  - Prometheus: `/prometheus`
  - MailHog: `/mailhog`

## Configuration

### Environment Variables

Each service uses ConfigMaps and Secrets for configuration. Key settings include:

#### Database Configuration
- Connection strings are configured for each service
- Default credentials: `postgres/postgres` (change in production!)

#### Security
- JWT secrets and encryption keys are base64 encoded in Secrets
- All secrets should be changed for production deployment

#### Scaling
- Default replica counts: 2 per service
- HPA scales based on CPU (70%) and memory (80%) utilization
- Maximum replicas vary by service load expectations

### Customization

1. **Update connection strings** in service YAML files
2. **Change security secrets** in Secret resources
3. **Modify resource limits** based on your cluster capacity
4. **Update domain names** in ingress configuration
5. **Adjust scaling parameters** in HPA files

## Production Deployment

### Security Checklist

1. **Change all default passwords and secrets**
   ```bash
   # Generate new secrets
   echo -n "your-new-secret" | base64
   ```

2. **Configure SSL certificates**
   - Install cert-manager for automatic certificate management
   - Update ingress annotations for your certificate issuer

3. **Network security**
   - Use network policies to restrict inter-pod communication
   - Configure firewalls for your cluster

4. **Resource limits**
   - Set appropriate CPU and memory limits for all containers
   - Configure storage classes for persistent volumes

### Database Setup

For production, consider:
1. **External database**: Use managed PostgreSQL service
2. **Backup strategy**: Configure regular database backups
3. **High availability**: Set up PostgreSQL clustering

### Monitoring

1. **Configure alerts** in Prometheus
2. **Set up notification channels** in Grafana
3. **Monitor resource usage** and adjust limits as needed

## Maintenance

### Updates

To update the application:

1. Build new Docker images with immutable tags
2. Update deployment images with `kubectl set image` or CI/CD
3. Apply updates:
   ```bash
   kubectl set image deployment/identity-service identity-api=registry.example.com/blockticket/identity-api:<git-sha> -n blockticket
   ```

### Scaling

Manual scaling:
```bash
kubectl scale deployment identity-service --replicas=5 -n blockticket
```

Auto-scaling is configured via HPA files in `scaling/`.

### Backup

Database backup example:
```bash
kubectl exec -it postgres-pod -n blockticket -- pg_dump -U postgres blockticket_identity > backup.sql
```

### Logs

View application logs:
```bash
kubectl logs -f deployment/identity-service -n blockticket
```

## Troubleshooting

### Common Issues

1. **Pods not starting**
   ```bash
   kubectl describe pod <pod-name> -n blockticket
   kubectl logs <pod-name> -n blockticket
   ```

2. **Database connection issues**
   - Check if PostgreSQL pod is running
   - Verify connection strings in ConfigMaps
   - Check network policies

3. **Image pull errors**
   - Ensure images are built and tagged correctly
   - Check image registry access
   - Verify image pull secrets if using private registry

4. **Ingress not working**
   - Ensure ingress controller is installed
   - Check DNS configuration
   - Verify SSL certificates

### Debugging Commands

```bash
# Check all resources
kubectl get all -n blockticket

# Check persistent volumes
kubectl get pv,pvc -n blockticket

# Check events
kubectl get events -n blockticket --sort-by='.lastTimestamp'

# Port forward for testing
kubectl port-forward service/identity-service 8080:80 -n blockticket
```

## Cleanup

To completely remove the application:

```bash
./scripts/destroy.sh
```

⚠️ **Warning**: This will permanently delete all data and resources!

## Support

For issues and questions:
1. Check the troubleshooting section above
2. Review Kubernetes logs for error messages
3. Verify all prerequisites are met
4. Check resource limits and cluster capacity

## License

This deployment configuration is part of the BlockTicket project.










