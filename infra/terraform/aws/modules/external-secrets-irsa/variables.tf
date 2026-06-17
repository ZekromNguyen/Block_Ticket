variable "name" {
  description = "Name prefix for IAM resources."
  type        = string
}

variable "oidc_provider_arn" {
  description = "EKS OIDC provider ARN."
  type        = string
}

variable "oidc_provider_url" {
  description = "EKS OIDC provider URL without https://."
  type        = string
}

variable "namespace" {
  description = "Kubernetes namespace for External Secrets."
  type        = string
  default     = "external-secrets"
}

variable "service_account_name" {
  description = "External Secrets service account name."
  type        = string
  default     = "external-secrets"
}

variable "secret_arns" {
  description = "Secrets Manager secret ARNs External Secrets may read."
  type        = list(string)
}

variable "tags" {
  description = "Resource tags."
  type        = map(string)
  default     = {}
}
