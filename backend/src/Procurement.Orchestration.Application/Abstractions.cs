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

public interface IWorkflowStarter
{
    Task<string> StartPurchaseRequestWorkflow(
        string purchaseRequestId,
        decimal spendAmount,
        decimal contractValue,
        string currentAssignee,
        string approvalStatus,
        string[] part1dAuthorizers,
        string[] part2aAuthorizers,
        CancellationToken ct);
}

public enum TaskTokenType
{
    BidEvaluationFormH,
    CabinetApprovalDocument,
    CabinetReportDocument,
    Part1dApproval,
    Part2aApproval
}

public interface ITaskTokenStore
{
    Task SaveToken(string purchaseRequestId, TaskTokenType tokenType, string taskToken, CancellationToken ct);
    Task<string?> GetToken(string purchaseRequestId, TaskTokenType tokenType, CancellationToken ct);
    Task DeleteToken(string purchaseRequestId, TaskTokenType tokenType, CancellationToken ct);
}

public interface ITaskTokenCallback
{
    Task SendSuccess(string taskToken, object output, CancellationToken ct);
}

