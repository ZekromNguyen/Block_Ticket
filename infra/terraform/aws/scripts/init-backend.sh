#!/usr/bin/env bash
#
# Usage:
#   ./scripts/init-backend.sh <dev|staging|prod>
#
# Runs from the environment directory, e.g.:
#   cd infra/terraform/aws/environments/dev
#   ../scripts/init-backend.sh dev
#
# Requires:
#   - terraform >= 1.6
#   - AWS credentials available in the environment (env vars, profile, OIDC, etc.)
#   - The blockticket tf-state S3 bucket and lock DynamoDB table to already exist
#     (apply infra/terraform/aws/bootstrap once before running this).
#
set -euo pipefail

ENV="${1:-}"
if [[ -z "${ENV}" ]]; then
  echo "Usage: $0 <dev|staging|prod>" >&2
  exit 1
fi

case "${ENV}" in
  dev)    STATE_KEY="envs/dev/terraform.tfstate"    ;;
  staging) STATE_KEY="envs/staging/terraform.tfstate" ;;
  prod)   STATE_KEY="envs/prod/terraform.tfstate"   ;;
  *)
    echo "Unknown environment: ${ENV}" >&2
    echo "Expected one of: dev, staging, prod" >&2
    exit 1
    ;;
esac

# Resolve the bootstrap state to discover bucket/region/table without hard-coding.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BOOTSTRAP_DIR="$(cd "${SCRIPT_DIR}/../bootstrap" && pwd)"

if [[ ! -d "${BOOTSTRAP_DIR}" ]]; then
  echo "Bootstrap module not found at ${BOOTSTRAP_DIR}" >&2
  exit 1
fi

echo "==> Reading bootstrap state to discover backend resources..."
pushd "${BOOTSTRAP_DIR}" >/dev/null

# Refuse to proceed if the bootstrap module has never been applied.
if ! terraform output -raw state_bucket >/dev/null 2>&1; then
  echo "Bootstrap state is empty. Run from ${BOOTSTRAP_DIR}:" >&2
  echo "    terraform init && terraform apply" >&2
  exit 1
fi

BUCKET="$(terraform output -raw state_bucket)"
REGION="$(terraform output -raw aws_region 2>/dev/null || true)"
if [[ -z "${REGION}" ]]; then
  REGION="${AWS_REGION:-ap-southeast-1}"
fi
LOCK_TABLE="$(terraform output -raw lock_table)"
popd >/dev/null

# Normalise region (some providers are case sensitive).
REGION="$(echo "${REGION}" | tr '[:upper:]' '[:lower:]')"

echo "==> Initialising ${ENV} backend"
echo "    bucket       = ${BUCKET}"
echo "    key          = ${STATE_KEY}"
echo "    region       = ${REGION}"
echo "    lock_table   = ${LOCK_TABLE}"
echo

cd "$(dirname "${BASH_SOURCE[0]}")/../environments/${ENV}"

terraform init \
  -backend-config="bucket=${BUCKET}" \
  -backend-config="key=${STATE_KEY}" \
  -backend-config="region=${REGION}" \
  -backend-config="dynamodb_table=${LOCK_TABLE}" \
  -backend-config="encrypt=true"

echo
echo "==> Backend initialised. Next:"
echo "    terraform validate"
echo "    terraform plan   # review the diff"
echo "    terraform apply  # gated by the ${ENV} GitHub Environment in CI"
