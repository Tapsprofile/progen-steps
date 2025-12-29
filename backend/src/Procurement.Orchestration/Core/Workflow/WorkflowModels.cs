namespace Procurement.Orchestration.Core.Workflow;

public enum WorkflowExecutionStatus
{
    Running = 0,
    Waiting = 1,
    Succeeded = 2,
    Failed = 3
}

public enum WorkflowStepType
{
    PhaseEnter = 0,
    PhaseComplete = 1,
    TokenIssued = 2,
    TokenCompleted = 3,
    Error = 4
}

public enum WorkflowStepStatus
{
    Ok = 0,
    Waiting = 1,
    Failed = 2
}

public sealed record WorkflowExecution(
    string ExecutionId,
    string PurchaseRequestId,
    string DefinitionName,
    int DefinitionVersion,
    WorkflowExecutionStatus Status,
    int PhaseIndex,
    object Data,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record WorkflowStep(
    string StepId,
    string ExecutionId,
    string PurchaseRequestId,
    string StepName,
    WorkflowStepType StepType,
    string? CorrelationId,
    object? Input,
    object? Output,
    WorkflowStepStatus Status,
    DateTimeOffset OccurredAtUtc);

public sealed record PendingTask(
    string TaskToken,
    string PurchaseRequestId,
    string TaskType,
    string? ActorId,
    string Description,
    DateTimeOffset CreatedAtUtc);

public sealed record WorkflowExecutionView(
    WorkflowExecution Execution,
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyList<PendingTask> PendingTasks);

