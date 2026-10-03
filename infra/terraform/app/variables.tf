variable "aws_region" {
  description = "AWS region everything is deployed to. Must match the bootstrap config, and must be the account's selected sign-up region: the AWS-managed SCP denies Lambda in every other region."
  type        = string
  default     = "us-east-2"
}

variable "lambda_function_name" {
  description = "Must match lambda_function_name in the bootstrap config -- that's what the deploy user's permissions are scoped to."
  type        = string
  default     = "cash-track-api"
}

variable "lambda_execution_role_arn" {
  description = "Output of the bootstrap config's lambda_execution_role_arn. This config never creates or modifies IAM itself."
  type        = string
}

variable "lambda_zip_path" {
  description = "Path to the zip produced by `dotnet lambda package`, built fresh in CI immediately before each apply."
  type        = string
}

variable "firebase_project_id" {
  description = "Firebase/GCP project id."
  type        = string
  default     = "cash-track-83c77"
}

variable "firebase_credentials_json" {
  description = "Full JSON content of the Firebase service account key."
  type        = string
  sensitive   = true
}
