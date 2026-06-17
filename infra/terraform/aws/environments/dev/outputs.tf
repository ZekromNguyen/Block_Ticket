output "cluster_name" {
  value = module.platform.cluster_name
}

output "ecr_repository_urls" {
  value = module.platform.ecr_repository_urls
}

output "external_secrets_role_arn" {
  value = module.external_secrets_irsa.role_arn
}

output "secret_arn" {
  value = module.platform.secret_arn
}
