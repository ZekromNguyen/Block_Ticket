#!/bin/bash

set -euo pipefail

cat <<'MSG'
Production setup moved to the Phase 2 Terraform/Kustomize flow.

Use:
  infra/terraform/aws/bootstrap
  infra/terraform/aws/scripts/init-backend.sh prod
  infra/terraform/aws/environments/prod
  k8s/overlays/prod

Secrets are managed in AWS Secrets Manager and synced by External Secrets
Operator. This script no longer mutates Kubernetes YAML with base64 secrets.
MSG
