# Procurement Orchestration Engine (starter)

This repo contains:

- **Workflow (ASL)**: a Step Functions state machine implementing spend-threshold routing, finance/legal parallel review, cabinet dual-document waits, a Form H task-token pause, procurement parallel tasks (E1–E6), and dynamic authoriser Maps (Part 1d + Part 2a).
- **Seed data (JSON)**: “worst case” input (Spend \(\ge 2.4M\)) to exercise the highest-value branch.
- **Backend (.NET 8)**: CQRS-style API skeleton to create purchase requests, start executions, and submit callbacks (SendTaskSuccess).
- **UI (Vue 3)**: a minimal screen to create a request and submit Form H.

## Workflow assets

- **ASL**: `workflow/asl/purchase-request-orchestration.asl.json`
- **Thresholds lookup**: `workflow/seed/thresholds.json`
- **Worst-case execution input**: `workflow/seed/worst-case-execution-input.json`

### Data flow guarantees

- The state machine keeps `PurchaseRequestId` at the root throughout (task results are written under `$.State.*` via `ResultPath`).
- A **catch-all** on the top-level `Parallel` routes any error to `arn:aws:states:::sns:publish` using `Config.Sns.WorkflowFailedTopicArn`.

### Human-in-the-loop waits

The ASL uses `.waitForTaskToken` for:

- Cabinet Approval document upload
- Cabinet Report document upload
- Bid Evaluation Form H (post-evaluation pause)
- Part 1d approvals (Map)
- Part 2a approvals (Map)

**Implementation note**: the Lambda invoked by each `.waitForTaskToken` step must persist the generated `TaskToken` so an external API can later call `SendTaskSuccess`.

## Backend (CQRS + callbacks)

Location: `backend/`

Run locally:

```bash
dotnet run --project backend/src/Procurement.Orchestration.Api
```

Key endpoints:

- `POST /purchase-requests` starts the workflow
- `POST /callbacks/bid-evaluation` submits Form H (requires stored token)
- `POST /debug/task-tokens` dev helper to seed a token for callback testing

## UI (Vue 3)

Location: `ui/`

Run locally:

```bash
cd ui
npm install
npm run dev
```

Configure API URL:

```bash
export VITE_API_BASE_URL="http://localhost:5000"
```