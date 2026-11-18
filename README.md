# Cash Track API

Backend for **Cash Track**, a mobile-first personal finance app: log income and expenses, tag them, track recurring transactions, set monthly budgets per tag and see where the money goes.

Built with **.NET 10 / C#**, following **Domain-Driven Design**, deployed to **AWS Lambda** with **Terraform** and **GitHub Actions**.

> Frontend (React Native + Expo): [cash-tracker-app](https://github.com/V1ctorBarbosa/cash-tracker-app)

## Highlights

- **Transactions** (income and expense) with a tag and a payment method (Pix, cash, debit, credit, transfer, boleto).
- **Recurring transactions** (rent, subscriptions, salary) so they don't need to be entered every month.
- **Monthly budgets per tag**, compared against actual spend.
- **Dashboard and statistics**: income vs. expense, balance over time, spend by tag and by payment method, month-over-month comparison, by month or custom date range.
- **180+ unit tests** (xUnit, FluentAssertions, NSubstitute), run in CI before every deploy.

## Architecture

```
api/src
├── CashTrack.Domain          entities, value objects, invariants (no dependencies)
├── CashTrack.Application     use cases and the ports they depend on
├── CashTrack.Infrastructure  Firestore repositories, Firebase Admin
└── CashTrack.Api             minimal API endpoints, auth, error handling
api/test/CashTrack.Tests      mirrors src, one test file per class
```

- Dependencies point inward: **Api → Application → Domain**. Infrastructure implements the application's interfaces and is wired at the composition root.
- **Authentication** validates Firebase ID tokens (Google Sign-In). Every endpoint requires an authenticated user.
- **Firestore is reached only through this API** (Admin SDK). The Firestore security rules deny every direct client read and write.
- Single user today, with a domain ready for multiple users: every aggregate is scoped by user.

## Infrastructure and delivery

- **AWS Lambda behind a Function URL**, defined in **Terraform** and split in two configs: a `bootstrap` config applied by hand (IAM, execution role, zero-spend budget alarm) and an `app` config applied by CI. State lives in Terraform Cloud.
- **GitHub Actions** runs the test suite and deploys on every push to `master` that touches the API or its infrastructure.

Details: [infra/README.md](infra/README.md)

## How it is built

- **Design first.** The domain model, calculations, dashboard and MVP scope were written down before the code: see the [implementation document](docs/cash-track-implementation.md).
- **AI-assisted engineering.** Developed with Claude Code inside a workflow of specs, small reviewed changes, tests with every change and Conventional Commits. The AI writes alongside me; the design decisions and the review are mine.
- Manual API testing: Postman collection in [docs/endpoints-test/postman](docs/endpoints-test/postman).

## Running locally

Requires the .NET 10 SDK.

```bash
dotnet test api/CashTrack.slnx
dotnet run --project api/src/CashTrack.Api
```

Running the API against real data needs a Firebase service account, which is not included in the repository.
