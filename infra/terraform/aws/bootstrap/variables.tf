variable "project" {
  description = "Short project name used in resource names."
  type        = string
  default     = "blockticket"
}

variable "aws_region" {
  description = "AWS region for the state bucket."
  type        = string
  default     = "ap-southeast-1"
}

variable "state_retain_days" {
  description = "Number of days to retain old state versions."
  type        = number
  default     = 90
}

variable "tags" {
  description = "Additional resource tags."
  type        = map(string)
  default     = {}
}
