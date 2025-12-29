-- Handwritten Procurement Orchestration Engine (CQRS-ready) schema
-- Goal: every step is persisted so it can be returned in API responses.

-- Purchase Request (write model / aggregate snapshot)
CREATE TABLE IF NOT EXISTS PurchaseRequests (
  PurchaseRequestId        TEXT PRIMARY KEY,
  SpendAmount              NUMERIC NOT NULL,
  ContractValue            NUMERIC NOT NULL,
  CurrentAssignee          TEXT NOT NULL,
  ApprovalStatus           TEXT NOT NULL,
  CreatedBy                TEXT NOT NULL,
  CreatedAtUtc             TIMESTAMP WITH TIME ZONE NOT NULL
);

-- Workflow execution (orchestration instance)
CREATE TABLE IF NOT EXISTS WorkflowExecutions (
  ExecutionId              TEXT PRIMARY KEY,
  PurchaseRequestId        TEXT NOT NULL REFERENCES PurchaseRequests(PurchaseRequestId),
  DefinitionName           TEXT NOT NULL,
  DefinitionVersion        INTEGER NOT NULL,
  Status                  TEXT NOT NULL, -- Running|Waiting|Succeeded|Failed
  PhaseIndex               INTEGER NOT NULL,
  DataJson                 JSONB NOT NULL, -- workflow data (includes CurrentAssignee/ApprovalStatus)
  CreatedAtUtc             TIMESTAMP WITH TIME ZONE NOT NULL,
  UpdatedAtUtc             TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_WorkflowExecutions_PurchaseRequestId
  ON WorkflowExecutions(PurchaseRequestId);

-- Step history (append-only)
CREATE TABLE IF NOT EXISTS WorkflowSteps (
  StepId                  TEXT PRIMARY KEY,
  ExecutionId             TEXT NOT NULL REFERENCES WorkflowExecutions(ExecutionId),
  PurchaseRequestId       TEXT NOT NULL,
  StepName                TEXT NOT NULL, -- e.g. routeBySpend, cabinetDocsIfRequired, waitForFormH, authorisationMap
  StepType                TEXT NOT NULL, -- PhaseEnter|PhaseComplete|TokenIssued|TokenCompleted|Error
  CorrelationId           TEXT NULL,      -- e.g. TaskToken or AuthorizerId
  InputJson               JSONB NULL,
  OutputJson              JSONB NULL,
  Status                  TEXT NOT NULL,  -- Ok|Waiting|Failed
  OccurredAtUtc           TIMESTAMP WITH TIME ZONE NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_WorkflowSteps_ExecutionId_OccurredAtUtc
  ON WorkflowSteps(ExecutionId, OccurredAtUtc);

-- Pending tasks (task tokens to resume)
CREATE TABLE IF NOT EXISTS WorkflowTaskTokens (
  TaskToken               TEXT PRIMARY KEY,
  ExecutionId             TEXT NOT NULL REFERENCES WorkflowExecutions(ExecutionId),
  PurchaseRequestId       TEXT NOT NULL,
  TaskType                TEXT NOT NULL, -- DOCUMENT_UPLOAD|FORM_H|APPROVAL_PART_1D|APPROVAL_PART_2A
  ActorId                 TEXT NULL,     -- authorizer id if applicable
  Description             TEXT NOT NULL,
  CreatedAtUtc            TIMESTAMP WITH TIME ZONE NOT NULL,
  CompletedAtUtc          TIMESTAMP WITH TIME ZONE NULL,
  CallbackOutputJson      JSONB NULL
);

CREATE INDEX IF NOT EXISTS IX_WorkflowTaskTokens_PurchaseRequestId_CompletedAtUtc
  ON WorkflowTaskTokens(PurchaseRequestId, CompletedAtUtc);

