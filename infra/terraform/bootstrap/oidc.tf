# A plain IAM user + access key rather than GitHub OIDC federation: the AWS account is on the
# "Free Plan", whose AWS-managed Service Control Policy denies iam:CreateOpenIDConnectProvider.
# A static key is a real security downgrade (long-lived, no auto-rotation), so the permissions
# below are kept as narrow as they would be under OIDC. If the account ever leaves the Free Plan,
# switching to OIDC is the better setup.
resource "aws_iam_user" "github_actions_deploy" {
  name = "cash-track-api-github-actions-deploy"
}

resource "aws_iam_access_key" "github_actions_deploy" {
  user = aws_iam_user.github_actions_deploy.name
}

# Deliberately narrow: only the one Lambda function this repo deploys, and only PassRole for the
# one execution role it needs to attach -- not broad Lambda or IAM access. Managing the execution
# role itself stays a bootstrap-only, human-run operation.
data "aws_iam_policy_document" "github_actions_deploy" {
  statement {
    sid = "ManageTheOneLambdaFunction"
    actions = [
      "lambda:CreateFunction",
      "lambda:UpdateFunctionCode",
      "lambda:UpdateFunctionConfiguration",
      "lambda:GetFunction",
      "lambda:GetFunctionConfiguration",
      "lambda:GetPolicy",
      "lambda:ListVersionsByFunction",
      "lambda:GetFunctionCodeSigningConfig",
      "lambda:GetFunctionConcurrency",
      "lambda:GetRuntimeManagementConfig",
      "lambda:AddPermission",
      "lambda:RemovePermission",
      "lambda:TagResource",
      "lambda:UntagResource",
      "lambda:ListTags",
      "lambda:CreateFunctionUrlConfig",
      "lambda:UpdateFunctionUrlConfig",
      "lambda:GetFunctionUrlConfig",
      "lambda:DeleteFunctionUrlConfig",
    ]
    resources = [
      "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.lambda_function_name}",
    ]
  }

  statement {
    sid       = "PassLambdaExecutionRoleOnly"
    actions   = ["iam:PassRole"]
    resources = [aws_iam_role.lambda_execution.arn]

    condition {
      test     = "StringEquals"
      variable = "iam:PassedToService"
      values   = ["lambda.amazonaws.com"]
    }
  }
}

resource "aws_iam_user_policy" "github_actions_deploy" {
  name   = "deploy-cash-track-api"
  user   = aws_iam_user.github_actions_deploy.name
  policy = data.aws_iam_policy_document.github_actions_deploy.json
}

data "aws_caller_identity" "current" {}
