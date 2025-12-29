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

public sealed record PendingTask(
    string TaskToken,
    string PurchaseRequestId,
    string TaskType,
    string? ActorId,
    string Description,
    DateTimeOffset CreatedAtUtc);

public sealed record WorkflowStartResult(
    string ExecutionId,
    string PurchaseRequestId,
    WorkflowExecutionStatus Status,
    IReadOnlyList<PendingTask> PendingTasks);

public sealed record WorkflowResumeResult(
    string ExecutionId,
    string PurchaseRequestId,
    WorkflowExecutionStatus Status,
    IReadOnlyList<PendingTask> PendingTasks);

public interface IWorkflowRuntime
{
    Task<WorkflowStartResult> StartPurchaseRequestWorkflow(object input, CancellationToken ct);
    Task<WorkflowResumeResult> ResumeByTaskToken(string taskToken, object output, CancellationToken ct);
    Task<IReadOnlyList<PendingTask>> ListPendingTasks(string purchaseRequestId, CancellationToken ct);
}

