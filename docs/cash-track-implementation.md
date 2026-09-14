# Cash Track — Implementation Document

## 1. Overview

Personal, mobile-first application for managing personal finances: monthly income, expenses, and how money moves between them over time.

Main goals:

- Log income (salary and other sources) as it comes in, typically monthly.
- Log expenses whenever they happen.
- Tag every transaction (income or expense) with a category, to group and analyze where money comes from and where it goes — including money set aside as personal savings, which is tracked as a tag on an expense rather than as a separate account (Section 8).
- Record the payment method used for each transaction (Pix, cash, debit, credit, transfer, boleto).
- Track recurring transactions (e.g. rent, subscriptions, recurring salary) so they don't need to be re-entered every month.
- Set a monthly budget goal per tag and see actual spend against it.
- Display charts and metrics: income vs. expense, balance over time, spend by tag, spend by payment method, budget vs. actual, and month-over-month comparison.
- Support views by month and by custom date range.
- Start as a personal application, single user, but keep the domain ready for future multi-user usage — same principle as Food Track.

The application must remain simple in the MVP. The intent is not to build a professional accounting system.

---

# 2. Architecture

## Stack

### Frontend
- React Native
- TypeScript
- Expo

### Backend
- .NET / C#
- REST API

### Infrastructure
- Firebase Authentication (Google Sign-In)
- Cloud Firestore
- Firebase Admin SDK on the backend

### Future infrastructure
- AWS, provisioned with Terraform — only after the application is validated locally. The `infra/` folder is reserved for this but stays empty until that phase starts.

## Flow

```text
┌─────────────────────────┐
│   React Native + Expo   │
│       TypeScript        │
└────────────┬────────────┘
             │ HTTPS / JSON
             ▼
┌─────────────────────────┐
│        .NET API         │
│                         │
│ Auth / Authorization    │
│ Transactions            │
│ Tags                    │
│ Recurring transactions  │
│ Budgets                 │
│ Dashboard               │
└────────────┬────────────┘
             │
             │ Firebase Admin SDK
             ▼
┌─────────────────────────┐
│       Firestore         │
│                         │
│ Users                   │
│ Transactions            │
│ Tags                    │
│ RecurringTransactions   │
│ Budgets                 │
└─────────────────────────┘
```

## Main architectural rule

The frontend will NOT access Firestore directly.

The frontend only talks to the .NET API.

The .NET API is responsible for:

- authentication/authorization;
- user identification;
- Firestore access;
- business rules (recurring transaction generation, budget calculation);
- aggregated dashboard data.

This avoids exposing secrets/credentials and keeps the application domain on the backend — same rule as Food Track.

---

# 3. Authentication

The first version is used only by the owner, but the model must be ready for multiple users.

Chosen technology:

- Firebase Authentication, Google Sign-In — same provider and flow already used in Food Track's app.

The API must validate the authenticated user's identity before accessing data belonging to them.

## Critical rule

Never trust a `userId` sent by the frontend.

```http
POST /transactions
Authorization: Bearer <token>
```

The API validates the token and obtains:

```text
authenticatedUserId = 019...
```

Then creates:

```text
Transaction.UserId = authenticatedUserId
```

Even if the frontend sends a different `userId`, the API must not allow that value to change the entity's owner.

---

# 4. Identifiers

Users and entities must have independent IDs.

```text
User
id = user identifier

Transaction
id = transaction identifier
userId = owner
```

`userId` identifies the user. `transactionId` identifies the transaction. `transactionId` must not be used to infer the user. GUID/UUID is a suitable option; the exact format can be defined during implementation.

---

# 5. Conceptual model

Main entities:

```text
User
Transaction
Tag
RecurringTransaction
Budget
```

`PaymentMethod` is a closed, fixed value (Section 8), not a persisted entity.

Relationships:

```text
User
 ├── Transactions
 │      ├── Tag
 │      └── PaymentMethod (fixed value)
 │
 ├── RecurringTransactions
 │      └── generates ──▶ Transactions
 │
 ├── Tags
 │
 └── Budgets
        └── Tag
```

The dashboard is not a persisted entity.

---

# 6. User

Represents the user's profile.

Conceptual fields:

```text
User
--------------------------
id
name
email
```

Example:

```json
{
  "id": "019...",
  "name": "Victor",
  "email": "vicfer2708@gmail.com"
}
```

There is no stored "monthly salary" field on `User` — income is logged as a `Transaction` (Section 9) like any other movement, since it can vary month to month (bonus, freelance, raise). Current balance is never stored on `User` either; it is always derived from the transaction history (Section 14).

---

# 7. Tag

Represents a category the user assigns to a transaction, for grouping and analysis. Used on both income and expense transactions — e.g. `Salário` and `Freelance` on income, `Mercado` and `Lazer` on expense.

Conceptual fields:

```text
Tag
--------------------------
id
userId
name
color
```

`color` exists purely for chart legends (Section 20) — each tag gets a consistent color across every chart it appears in.

Example set of tags (user-defined, not seeded by the system): `Salário`, `Freelance`, `Mercado`, `Moradia`, `Transporte`, `Lazer`, `Saúde`, `Assinaturas`, `Poupança`, `Outros`.

## Poupança (personal savings)

Per your decision, personal savings are not a separate account/entity — "money set aside" is logged as a normal expense `Transaction` tagged `Poupança` (or however you choose to name it). This keeps the domain simple: the amount saved in a period is just `sum(Transactions where type = Expense and tagId = poupançaTagId)`, the same query shape used for every other tag (Section 19).

---

# 8. PaymentMethod

Represents how a transaction was paid. Modeled as a **fixed, closed enumeration**, not a user-managed entity — this is an assumption made to keep the MVP simple; flag it for review (Section 30 lists it as an open decision).

```text
PaymentMethod
--------------------------
Pix
Dinheiro
CartaoDebito
CartaoCredito
TransferenciaBancaria
Boleto
Outro
```

If you later need per-card or per-bank tracking (e.g. "Nubank Crédito" vs. "Itaú Crédito" as distinct groupings), `PaymentMethod` would need to become a user-managed entity shaped like `Tag`. That is listed as a future feature (Section 29), not built now.

---

# 9. Transaction

Represents a single financial movement — an income or an expense. Both share the same shape, so they are modeled as one entity with a `type` discriminator rather than as two separate entities: the fields, validation, and queries (sum by tag, sum by month) are identical either way, and the two kinds will never diverge in structure. This mirrors how Food Track keeps a single `Meal` entity with a `mealType` enum instead of splitting into `Breakfast`/`Lunch`/`Dinner` entities.

Conceptual fields:

```text
Transaction
--------------------------
id
userId
type
amount
date
description
tagId
paymentMethod
recurringTransactionId
createdAt
```

```text
type: Income | Expense
```

Example:

```json
{
  "id": "019...",
  "userId": "019...",
  "type": "Expense",
  "amount": 89.90,
  "date": "2026-09-10",
  "description": "Supermercado",
  "tagId": "tag-mercado",
  "paymentMethod": "CartaoDebito",
  "recurringTransactionId": null,
  "createdAt": "2026-09-10T18:32:00Z"
}
```

`amount` is always stored as a positive value (Section 10) — the sign is derived from `type` when computing totals, never stored redundantly. `recurringTransactionId` is set only when the transaction was generated from a `RecurringTransaction` (Section 11); it is `null` for transactions logged directly by the user.

`date` is the date the movement happened (editable by the user, e.g. backdating a purchase); `createdAt` is the server timestamp of when the record was created, kept for audit purposes and never shown as the primary date in the UI.

---

# 10. Money

`amount` is a decimal value object, always non-negative. Currency is fixed to BRL for the MVP, per your decision — no currency field is persisted. Adding a currency code later is a non-breaking addition, since every existing record is implicitly BRL.

---

# 11. RecurringTransaction

Represents a transaction definition that repeats every month — rent, subscriptions, a fixed recurring salary.

Conceptual fields:

```text
RecurringTransaction
--------------------------
id
userId
type
amount
description
tagId
paymentMethod
dayOfMonth
startDate
endDate
active
```

Example:

```json
{
  "id": "019...",
  "userId": "019...",
  "type": "Expense",
  "amount": 1800.00,
  "description": "Aluguel",
  "tagId": "tag-moradia",
  "paymentMethod": "TransferenciaBancaria",
  "dayOfMonth": 5,
  "startDate": "2026-01-01",
  "endDate": null,
  "active": true
}
```

`endDate` is `null` for an indefinite recurrence. `active` lets the user pause a recurrence without deleting its definition or its history of already-generated transactions.

---

# 12. Recurring generation rule

The MVP has no background scheduler (no infra for that exists yet — Section 2 defers AWS/Terraform to a later phase). Generation is **lazy**, done by the API at query time:

Whenever `transactions` or `dashboard` is queried for a range covering a given month, for each `RecurringTransaction` that is `active` and whose `[startDate, endDate]` covers that month, the API checks whether a `Transaction` with that `recurringTransactionId` already exists for the month. If not, it materializes one, dated `dayOfMonth` (clamped to the last day of shorter months, e.g. day 31 becomes day 28/29 in February), before answering the query.

## Important

Each materialized month is a real, independent `Transaction` record — editable or deletable on its own (e.g. if rent goes up one specific month, or a subscription is cancelled after being charged once) without affecting the `RecurringTransaction` definition or other months already generated.

---

# 13. Budget

Represents a monthly spending goal for a tag.

Conceptual fields:

```text
Budget
--------------------------
id
userId
tagId
month
limitAmount
```

Example:

```json
{
  "id": "019...",
  "userId": "019...",
  "tagId": "tag-lazer",
  "month": "2026-09",
  "limitAmount": 500.00
}
```

One `Budget` per `(userId, tagId, month)` — setting a new goal for a tag/month that already has one replaces it rather than creating a duplicate.

Nothing at the domain level restricts a `Budget` to expense tags, but the dashboard (Section 19) only ever compares it against expense totals, since a goal only makes sense as a spending ceiling.

---

# 14. Persisted vs. derived data

Fundamental principle:

> Persist facts, not visualizations.

Persist:

```text
Income or expense that happened, with its date, amount, tag, and payment method
Recurring transaction definitions
Budget goals per tag/month
```

Do not persist:

```text
Current balance
Monthly total income
Monthly total expense
Spend by tag
Spend by payment method
Budget vs. actual
Month-over-month trend
```

All of this is derived from `Transaction` history on every request.

---

# 15. Full month example

User logs during September:

```text
09/01 → Income   R$ 5,200.00  Salário         Pix
09/05 → Expense  R$ 1,800.00  Aluguel         Transferência   (recurring)
09/07 → Expense  R$    89.90  Mercado         Cartão Débito
09/10 → Expense  R$   45.00   Lazer           Pix
09/15 → Expense  R$   35.90   Assinaturas     Cartão Crédito  (recurring)
09/20 → Expense  R$   300.00  Poupança        Transferência
```

The backend aggregates:

```text
TotalIncome   = 5,200.00
TotalExpense  = 1,800.00 + 89.90 + 45.00 + 35.90 + 300.00 = 2,270.80
Balance       = TotalIncome - TotalExpense = 2,929.20
```

Grouped by tag (expenses only, for the budget/spend-by-tag charts):

```text
Aluguel        1,800.00
Mercado           89.90
Lazer             45.00
Assinaturas       35.90
Poupança         300.00
```

The dashboard can show all of the above, plus the same numbers charted against prior months (Section 20).

---

# 16. Conceptual Firestore structure

```text
users/
    {userId}

transactions/
    {transactionId}

tags/
    {tagId}

recurringTransactions/
    {recurringTransactionId}

budgets/
    {budgetId}
```

Every document below `users/` carries a `userId` field rather than living in a nested subcollection — same top-level-collection-plus-`userId`-field shape used in Food Track. The exact physical decision can be revisited during implementation; what matters is keeping the conceptual entities:

```text
User
Transaction
Tag
RecurringTransaction
Budget
```

---

# 17. Relationship between user and data

Every entity directly belonging to a user carries a `userId`, exactly as in Food Track:

```text
User
id = U1

Transaction
id = T1
userId = U1

Tag
id = TAG1
userId = U1

RecurringTransaction
id = R1
userId = U1

Budget
id = B1
userId = U1
```

This allows, in the future, multiple users without remodeling the domain.

---

# 18. Conceptual endpoints

```text
GET    /profile
PUT    /profile

GET    /tags
POST   /tags
PUT    /tags/{id}
DELETE /tags/{id}

GET    /transactions?from=...&to=...&tagId=...&type=...&paymentMethod=...
GET    /transactions/{id}
POST   /transactions
PUT    /transactions/{id}
DELETE /transactions/{id}

GET    /recurring-transactions
POST   /recurring-transactions
PUT    /recurring-transactions/{id}
DELETE /recurring-transactions/{id}

GET    /budgets?month=...
PUT    /budgets/{tagId}/{month}
DELETE /budgets/{tagId}/{month}

GET    /dashboard?from=...&to=...
```

The exact contracts will be defined during implementation. `PUT /budgets/{tagId}/{month}` uses an upsert semantic, matching the "one budget per tag/month, new value replaces the old one" rule from Section 13.

---

# 19. Dashboard API

```text
GET /dashboard?from=2026-01-01&to=2026-09-30
```

Conceptual response:

```json
{
  "totalIncome": 46800.00,
  "totalExpense": 20437.20,
  "balance": 26362.80,
  "monthlySummary": [
    {
      "month": "2026-08",
      "income": 5200.00,
      "expense": 2310.40
    },
    {
      "month": "2026-09",
      "income": 5200.00,
      "expense": 2270.80
    }
  ],
  "spendByTag": [
    { "tagId": "tag-moradia", "tagName": "Moradia", "amount": 1800.00 },
    { "tagId": "tag-mercado", "tagName": "Mercado", "amount": 89.90 }
  ],
  "spendByPaymentMethod": [
    { "paymentMethod": "TransferenciaBancaria", "amount": 2100.00 },
    { "paymentMethod": "Pix", "amount": 45.00 }
  ],
  "budgets": [
    {
      "tagId": "tag-lazer",
      "tagName": "Lazer",
      "month": "2026-09",
      "limitAmount": 500.00,
      "actualAmount": 45.00
    }
  ]
}
```

`totalIncome`, `totalExpense`, and `balance` cover the full `[from, to]` range. `monthlySummary` contains exactly one entry per calendar month in `[from, to]`, zero-filled for months with no transactions, so the frontend can render a continuous trend line without filling gaps itself — same rule Food Track applies to `nutritionHistory`.

`spendByTag` and `spendByPaymentMethod` aggregate expenses only, over the full range. `budgets` returns one entry per `Budget` whose `month` falls in `[from, to]`, each carrying its `limitAmount` alongside the `actualAmount` actually spent on that tag that month, so the frontend renders goal-vs-actual without a second request.

---

# 20. Metrics and charts overview

```text
Balance over time      — monthlySummary, income and expense as two lines (or income − expense as one)
Spend by tag            — spendByTag, as a pie or horizontal bar chart
Spend by payment method — spendByPaymentMethod, as a pie or bar chart
Budget vs. actual       — budgets, one bar pair (limit vs. actual) per tag, for the selected month
Month-over-month trend  — monthlySummary charted across a longer range (e.g. last 12 months)
```

Each tag keeps the same `color` (Section 7) across every chart it appears in, so "Lazer" is visually the same color on the spend-by-tag pie chart and the budget-vs-actual bars.

The frontend remains mainly responsible for presentation, same as Food Track: the API returns the aggregated numbers, the frontend decides layout and which charts to show together.

---

# 21. Initial UX

Dashboard:

```text
┌───────────────────────────────┐
│          DASHBOARD            │
│                               │
│ Setembro 2026                 │
│                               │
│ Receita     R$ 5.200,00       │
│ Despesa     R$ 2.270,80       │
│ Saldo       R$ 2.929,20       │
│                               │
│ ───────────────────────────── │
│                               │
│ Gasto por categoria            │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Gasto por forma de pagamento   │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Orçamento vs. real             │
│       chart                   │
│                               │
│ ───────────────────────────── │
│                               │
│ Últimos meses                  │
│       chart                   │
│                               │
└───────────────────────────────┘
```

Quick actions:

```text
+ Nova receita
+ Nova despesa
```

Note: user-facing app text is Portuguese (Section 27 of the workspace's project memory on localization), while this document, the API, and the codebase stay in English.

---

# 22. MVP features

## Do

- React Native.
- Expo.
- TypeScript.
- .NET API.
- Firebase Authentication (Google Sign-In).
- Firestore.
- Firebase Admin SDK.
- User profile.
- Tag CRUD.
- Transaction CRUD (income and expense).
- Payment method on each transaction (fixed enumeration).
- Recurring transactions, lazily materialized per month.
- Budget per tag/month.
- Dashboard: total income, total expense, balance.
- Spend-by-tag chart.
- Spend-by-payment-method chart.
- Budget-vs-actual chart.
- Month-over-month trend chart.
- Period filters (month picker, custom range).

## Not initially

- Multiple currencies.
- Bank/open-finance integration (automatic transaction import).
- Multi-user support (data model is ready, UI/sharing is not built).
- Shared/family budgets.
- Investment tracking.
- Bills/invoices with due-date reminders and notifications.
- User-managed payment methods (custom cards/accounts) — Section 8 keeps this a fixed enum for now.
- Advanced administration.
- Premature optimizations.
- Overly complex architecture.

---

# 23. Future features

Possibilities:

- User-managed payment methods (replacing the fixed enum from Section 8), each with its own name and optional balance.
- Notifications/reminders (e.g. a recurring bill due soon, a budget close to its limit).
- Offline support + sync.
- Multi-user support with shared budgets between accounts.
- Export to spreadsheet/PDF.
- Bank statement import (CSV or open-finance API).
- AWS infrastructure via Terraform (Section 2), once the application is validated locally.

---

# 24. Recommended implementation order

## Phase 1 — Foundation

1. Create React Native + Expo + TypeScript project (`app/`).
2. Create .NET Web API (`api/`), structured per DDD layers (Domain/Application/Infrastructure/Api), mirroring Food Track.
3. Configure Firebase project (Authentication + Firestore).
4. Configure Google Sign-In on the frontend and token validation on the backend.
5. Configure Firebase Admin SDK on the backend.

## Phase 2 — Tags & Transactions

6. Create `Tag` entity and CRUD.
7. Create `Transaction` entity and CRUD.
8. Implement `PaymentMethod` enum and validation.
9. Create transaction logging screen (income and expense).
10. Create transaction list/history screen, filterable by tag/type/payment method/period.

## Phase 3 — Recurring transactions

11. Create `RecurringTransaction` entity and CRUD.
12. Implement the lazy monthly generation rule (Section 12).
13. Create recurring transaction management screen.

## Phase 4 — Budgets

14. Create `Budget` entity and upsert endpoint.
15. Create budget management screen (set a limit per tag/month).

## Phase 5 — Dashboard

16. Create dashboard endpoint, aggregating income/expense/balance per month.
17. Implement spend-by-tag aggregation and chart.
18. Implement spend-by-payment-method aggregation and chart.
19. Implement budget-vs-actual aggregation and chart.
20. Implement month-over-month trend chart.
21. Add period filters (month picker, custom range).

## Phase 6 — Refinement

22. Improve mobile UX.
23. Add error handling.
24. Add loading/empty states.
25. Add validations.
26. Add key tests (unit tests per CLAUDE.md conventions, mirrored `test/` tree).

## Phase 7 — AWS infrastructure (later, once validated locally)

27. Design Terraform modules for the API's AWS infrastructure, mirroring `food-track/infra/terraform`.

---

# 25. Architectural principles

1. The frontend does not access Firestore directly.
2. The .NET API is the domain boundary.
3. The backend identifies the user from the authenticated token.
4. Never trust a `userId` sent by the frontend.
5. Entities belonging to a user carry a `userId`.
6. User IDs and entity IDs are independent.
7. `Transaction` unifies income and expense behind a `type` discriminator instead of two near-identical entities.
8. `amount` is always positive; sign is derived from `type`.
9. `PaymentMethod` is a fixed, closed value for the MVP — not a user-managed entity.
10. Recurring transactions are generated lazily, per query, with no scheduler.
11. Persist historical facts (transactions, recurring definitions, budgets).
12. Derive everything else — totals, balance, spend-by-tag, spend-by-payment-method, budget-vs-actual, month-over-month trend.
13. Avoid unnecessary complexity in the MVP.
14. Keep the domain ready for growth (multi-user, real payment method entities) without building features that have no value yet.

---

# 26. Final architectural summary

```text
React Native + Expo
        |
        | HTTPS / JSON
        v
     .NET API
        |
        +---------------------+
        |                     |
        v                     v
Firebase Auth            Firestore
                              |
                              +-- Users
                              +-- Transactions
                              +-- Tags
                              +-- RecurringTransactions
                              +-- Budgets
```

---

# 27. Open decisions to confirm

Two calls were made in this document without an explicit answer from you — flagging them here so you can confirm or override before implementation starts:

1. **`PaymentMethod` as a fixed enum (Section 8)**, not a user-managed entity like `Tag`. If you actually want to track specific cards/accounts (e.g. "Nubank Crédito" separately from "Itaú Crédito"), this should become an entity now instead of a future feature.
2. **`Transaction` as one entity with a `type` discriminator (Section 9)**, instead of separate `Income`/`Expense` entities. Given both share every field and query shape, this keeps the domain smaller — but it's worth confirming before the domain layer is built, since changing it later touches every layer.
