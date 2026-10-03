terraform {
  required_version = ">= 1.7"

  cloud {
    organization = "v1ctorbarbosa"

    workspaces {
      name = "cash-track-api"
    }
  }

  required_providers {
    aws = {
      source = "hashicorp/aws"
      # The 5.x provider rejects runtime = "dotnet10": its vendored AWS SDK predates that runtime.
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region = var.aws_region
}
