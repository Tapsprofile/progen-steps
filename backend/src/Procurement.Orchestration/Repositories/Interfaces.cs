using Procurement.Orchestration.Core.Domain;
using Procurement.Orchestration.Core.Workflow;

namespace Procurement.Orchestration.Repositories;

public interface IPurchaseRequestRepository
{
    Task Upsert(PurchaseRequest request, CancellationToken ct);
    Task<PurchaseRequest?> Get(string purchaseRequestId, CancellationToken ct);
}

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

