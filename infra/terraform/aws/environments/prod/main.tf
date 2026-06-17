terraform {
  required_version = ">= 1.6.0"

  # Backend configuration is supplied at `terraform init` time via
  # `../scripts/init-backend.sh prod` (or init-backend.ps1). Keep this block
  # empty here so the bucket, key, region, and lock-table credentials stay
  # out of source control and out of the module diffs.
  backend "s3" {}
}

provider "aws" {
  region = var.aws_region
}

# ... rest of module wiring unchanged

locals {
  name = "${var.project}-${var.environment}"
  tags = {
    Project     = var.project
    Environment = var.environment
    Owner       = "platform"
  }
}

module "platform" {
  source = "../../modules/platform"

  project                = var.project
  environment            = var.environment
  aws_region             = var.aws_region
  vpc_cidr               = var.vpc_cidr
  az_count               = var.az_count
  kubernetes_version     = var.kubernetes_version
  node_instance_types    = var.node_instance_types
  node_desired_size      = var.node_desired_size
  node_min_size          = var.node_min_size
  node_max_size          = var.node_max_size
  db_instance_class      = var.db_instance_class
  db_allocated_storage   = var.db_allocated_storage
  redis_node_type        = var.redis_node_type
  rabbitmq_instance_type = var.rabbitmq_instance_type
  allowed_cidr_blocks    = var.allowed_cidr_blocks
  tags                   = local.tags
}

module "external_secrets_irsa" {
  source = "../../modules/external-secrets-irsa"

  name              = local.name
  oidc_provider_arn = module.platform.oidc_provider_arn
  oidc_provider_url = module.platform.oidc_provider_url
  secret_arns       = [module.platform.secret_arn]
  tags              = local.tags
}
