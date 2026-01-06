# Procurement Orchestration Engine (starter)

This repo contains:

- **Handwritten workflow engine (no AWS Step Functions)**: a small in-process orchestration runtime that executes an ASL-inspired JSON definition, including spend-threshold routing, parallel “wait for X” phases, and dynamic authoriser Maps.
- **Seed data (JSON)**: “worst case” input (Spend \(\ge 2.4M\)) to exercise the highest-value branch.
- **Backend (.NET 8)**: CQRS-style API skeleton to create purchase requests, start executions, list pending task tokens, and submit callbacks to resume the workflow.
- **UI (Vue 3)**: a minimal screen to create a request and submit Form H.

## Workflow assets

- **Handwritten definition**: `workflow/handwritten/purchase-request-workflow.json`
- **Thresholds lookup**: `workflow/seed/thresholds.json`
- **Worst-case execution input**: `workflow/seed/worst-case-execution-input.json`

Legacy (not used): `workflow/legacy/aws-stepfunctions/`

### Data flow guarantees

- The state machine keeps `PurchaseRequestId` at the root throughout (task results are written under `$.State.*` via `ResultPath`).
- A **catch-all** on the top-level `Parallel` routes any error to `arn:aws:states:::sns:publish` using `Config.Sns.WorkflowFailedTopicArn`.

### Human-in-the-loop waits

The handwritten runtime issues **TaskTokens** for:

- Cabinet Approval document upload
- Cabinet Report document upload
- Bid Evaluation Form H (post-evaluation pause)
- Part 1d approvals (Map)
- Part 2a approvals (Map)

**Implementation note**: tokens are generated + tracked inside the API (in-memory in this starter). External systems resume by calling the callback endpoint with the `TaskToken`.

## Backend (CQRS + callbacks)

Location: `backend/`

Run locally:

```bash
dotnet run --project backend/src/Procurement.Orchestration.Api
```

Key endpoints:

- `POST /purchase-requests` starts the workflow
- `GET /purchase-requests/{id}/pending-tasks` lists task tokens to complete
- `POST /workflow/callbacks` resumes the workflow by `TaskToken`

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