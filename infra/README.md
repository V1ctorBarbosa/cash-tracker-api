# Infrastructure

The API deploys to AWS Lambda behind a Function URL (not API Gateway: its free tier lasts 12
months, while Lambda's is "Always Free"). Everything is defined in Terraform under `terraform/`,
split into two configs:

- **`terraform/bootstrap/`**: the IAM user GitHub Actions authenticates as (scoped to only this
  Lambda function), the Lambda function's own execution role, and a zero-spend budget alert.
  Applied once, by hand, from your own machine. GitHub Actions never touches this config.
  A plain IAM user + access key is used instead of GitHub OIDC because an account on the AWS "Free
  Plan" has an AWS-managed Service Control Policy that blocks `iam:CreateOpenIDConnectProvider`.
- **`terraform/app/`**: the Lambda function and its Function URL. Applied automatically by
  GitHub Actions on every push to `master` that touches `api/` or `infra/terraform/app/`.

This setup mirrors the `food-track` project's. The constraints in the AWS account (region
`us-east-2` only, `dotnet10` runtime needing the `~> 6.0` AWS provider) are the same, so the
region default stays `us-east-2`.

## One-time setup

### 1. Terraform Cloud (free, no card)

1. Create an organization named `cash-track` at [app.terraform.io](https://app.terraform.io)
   (or rename `organization` in both `main.tf` files to match the one you use).
2. Create two workspaces in it: `cash-track-api-bootstrap` and `cash-track-api`, both with the
   **CLI-Driven Workflow**. Terraform runs on your machine and in GitHub Actions; Terraform Cloud
   only stores state.
3. On both workspaces, *Settings → General → Execution mode → Local*. In this mode workspace
   variables set in the Terraform Cloud UI are ignored, so the app config's inputs come from the
   GitHub secrets below.
4. Generate a user API token (*User settings → Tokens*) and run `terraform login`, pasting it.

### 2. Apply the bootstrap config

```bash
cd infra/terraform/bootstrap
terraform init
terraform apply -var="budget_alert_email=you@example.com"
```

This needs real AWS credentials on your machine. It is the only time this project needs them
directly; everything after uses the IAM user it creates.

Outputs: `github_actions_access_key_id`, `lambda_execution_role_arn`, and
`github_actions_secret_access_key` (sensitive; reveal with
`terraform output -raw github_actions_secret_access_key`). If you lose the secret key,
`terraform taint aws_iam_access_key.github_actions_deploy` and re-apply.

### 3. Add GitHub repo secrets

On `V1ctorBarbosa/cash-tracker-api` → *Settings → Secrets and variables → Actions*:

| Secret | Value |
|---|---|
| `AWS_ACCESS_KEY_ID` | the `github_actions_access_key_id` output |
| `AWS_SECRET_ACCESS_KEY` | the `github_actions_secret_access_key` output |
| `TF_API_TOKEN` | the Terraform Cloud token from step 1 |
| `LAMBDA_EXECUTION_ROLE_ARN` | the `lambda_execution_role_arn` output |
| `FIREBASE_CREDENTIALS_JSON` | full contents of `firebase-service-account.json` |

`firebase_project_id` and `lambda_function_name` already default correctly in `variables.tf`.

Push to `master` and the `Deploy API` workflow tests, packages and deploys.

## Pointing the app at the deployed API

```bash
cd infra/terraform/app
terraform output -raw function_url
```

Set that value as `EXPO_PUBLIC_API_BASE_URL` when building the app (`app/src/shared/config/api-config.ts`
already reads it). `curl` the URL + `health` to confirm the function is live, or `health/firebase`
with a real Firebase ID token in the `Authorization: Bearer` header.
