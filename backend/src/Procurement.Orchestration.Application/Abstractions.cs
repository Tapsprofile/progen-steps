using Procurement.Orchestration.Domain;

namespace Procurement.Orchestration.Application;

public interface IPurchaseRequestRepository
{
    Task Upsert(PurchaseRequest request, CancellationToken ct);
    Task<PurchaseRequest?> Get(string purchaseRequestId, CancellationToken ct);
}

public interface IEventPublisher
{
    Task Publish(DomainEvent e, CancellationToken ct);
}

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

public sealed record WorkflowExecutionView(
    WorkflowExecution Execution,
    IReadOnlyList<WorkflowStep> Steps,
    IReadOnlyList<PendingTask> PendingTasks);

public sealed record WorkflowStartResult(
    WorkflowExecutionView View);

public sealed record WorkflowResumeResult(
    WorkflowExecutionView View);

public interface IWorkflowStore
{
    Task CreateExecution(WorkflowExecution execution, CancellationToken ct);
    Task UpdateExecution(WorkflowExecution execution, CancellationToken ct);

    Task AppendStep(WorkflowStep step, CancellationToken ct);

    Task UpsertPendingTask(string executionId, PendingTask task, CancellationToken ct);
    Task CompletePendingTask(string taskToken, object callbackOutput, CancellationToken ct);

    Task<WorkflowExecution?> GetExecution(string executionId, CancellationToken ct);
    Task<IReadOnlyList<WorkflowStep>> ListSteps(string executionId, CancellationToken ct);
    Task<IReadOnlyList<PendingTask>> ListPendingTasksByExecution(string executionId, CancellationToken ct);

    Task<(string ExecutionId, string PurchaseRequestId)?> FindExecutionByTaskToken(string taskToken, CancellationToken ct);
    Task<IReadOnlyList<PendingTask>> ListPendingTasksByPurchaseRequest(string purchaseRequestId, CancellationToken ct);
}

public interface IWorkflowRuntime
{
    Task<WorkflowStartResult> StartPurchaseRequestWorkflow(object input, CancellationToken ct);
    Task<WorkflowResumeResult> ResumeByTaskToken(string taskToken, object output, CancellationToken ct);
    Task<IReadOnlyList<PendingTask>> ListPendingTasks(string purchaseRequestId, CancellationToken ct);
    Task<WorkflowExecutionView?> GetExecutionView(string executionId, CancellationToken ct);
}

