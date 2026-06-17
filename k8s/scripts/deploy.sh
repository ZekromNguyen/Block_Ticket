#!/bin/bash

set -euo pipefail

ENVIRONMENT="${1:-dev}"

case "$ENVIRONMENT" in
  dev|staging|prod) ;;
  *)
    echo "Usage: $0 <dev|staging|prod>"
    exit 1
    ;;
esac

cat <<MSG
Phase 3 uses GitOps for cluster deployment.

For local/dev bootstrap only:
  kubectl apply -k k8s/overlays/${ENVIRONMENT}

For managed environments:
  kubectl apply -k k8s/gitops/argocd

ArgoCD will sync:
  k8s/overlays/${ENVIRONMENT}
  k8s/addons/observability

CI updates k8s/overlays/<env>/images.yaml after image signing and verification.
MSG
