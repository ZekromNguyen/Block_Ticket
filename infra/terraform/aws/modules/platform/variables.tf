variable "project" {
  description = "Short project name used in resource names."
  type        = string
}

variable "environment" {
  description = "Deployment environment name."
  type        = string
}

variable "aws_region" {
  description = "AWS region."
  type        = string
}

variable "vpc_cidr" {
  description = "VPC CIDR block."
  type        = string
}

variable "az_count" {
  description = "Number of availability zones to use."
  type        = number
  default     = 2
}

variable "kubernetes_version" {
  description = "EKS Kubernetes version."
  type        = string
  default     = "1.31"
}

variable "node_instance_types" {
  description = "Managed node group instance types."
  type        = list(string)
  default     = ["t3.medium"]
}

variable "node_desired_size" {
  description = "Desired worker node count."
  type        = number
  default     = 2
}

variable "node_min_size" {
  description = "Minimum worker node count."
  type        = number
  default     = 2
}

variable "node_max_size" {
  description = "Maximum worker node count."
  type        = number
  default     = 4
}

variable "db_instance_class" {
  description = "RDS PostgreSQL instance class."
  type        = string
  default     = "db.t4g.micro"
}

variable "db_allocated_storage" {
  description = "RDS allocated storage in GiB."
  type        = number
  default     = 20
}

variable "db_backup_retention_period" {
  description = "RDS backup retention period in days. Null keeps the environment default."
  type        = number
  default     = null
}

variable "db_deletion_protection" {
  description = "Whether deletion protection is enabled for RDS. Null keeps the environment default."
  type        = bool
  default     = null
}

variable "db_multi_az" {
  description = "Whether RDS uses Multi-AZ. Null keeps the environment default."
  type        = bool
  default     = null
}

variable "redis_node_type" {
  description = "ElastiCache Redis node type."
  type        = string
  default     = "cache.t4g.micro"
}

variable "redis_multi_az" {
  description = "Whether Redis automatic failover and Multi-AZ are enabled. Null keeps the environment default."
  type        = bool
  default     = null
}

variable "rabbitmq_instance_type" {
  description = "Amazon MQ RabbitMQ broker instance type."
  type        = string
  default     = "mq.t3.micro"
}

variable "rabbitmq_multi_az" {
  description = "Whether RabbitMQ uses a clustered Multi-AZ broker. Null keeps the environment default."
  type        = bool
  default     = null
}

variable "allowed_cidr_blocks" {
  description = "CIDR blocks allowed to reach public ingress and private admin endpoints."
  type        = list(string)
  default     = ["0.0.0.0/0"]
}

variable "tags" {
  description = "Additional resource tags."
  type        = map(string)
  default     = {}
}
