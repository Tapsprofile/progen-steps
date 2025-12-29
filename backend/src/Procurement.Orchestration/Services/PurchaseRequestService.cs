using Procurement.Orchestration.Core.Domain;
using Procurement.Orchestration.Core.Exceptions;
using Procurement.Orchestration.Core.Workflow;
using Procurement.Orchestration.Models;
using Procurement.Orchestration.Repositories;

namespace Procurement.Orchestration.Services;

public interface IPurchaseRequestService
{
    Task<CreatePurchaseRequestResponse> CreateAndStart(CreatePurchaseRequestRequest request, CancellationToken ct);
    Task<PurchaseRequestResponse> Get(string purchaseRequestId, CancellationToken ct);
}

public sealed class PurchaseRequestService(
    IPurchaseRequestRepository repo,
    IWorkflowRuntime workflow)
    : IPurchaseRequestService
{
    public async Task<CreatePurchaseRequestResponse> CreateAndStart(CreatePurchaseRequestRequest request, CancellationToken ct)
    {
        var id = string.IsNullOrWhiteSpace(request.PurchaseRequestId)
            ? $"PR-{DateTimeOffset.UtcNow:yyyy}-{Guid.NewGuid():N}".ToUpperInvariant()
            : request.PurchaseRequestId!;

        var createdBy = string.IsNullOrWhiteSpace(request.CreatedBy) ? "REQUESTOR" : request.CreatedBy!;

        var pr = PurchaseRequest.CreateAndSubmit(id, request.SpendAmount, request.ContractValue, createdBy);
        await repo.Upsert(pr, ct);

        var exec = await workflow.Start(new
        {
            PurchaseRequestId = pr.PurchaseRequestId,
            SpendAmount = pr.SpendAmount,
            ContractValue = pr.ContractValue,
            CurrentAssignee = pr.CurrentAssignee,
            ApprovalStatus = pr.ApprovalStatus.ToString(),
            Authorization = new
            {
                Part1dAuthorizers = request.Part1dAuthorizers ?? Array.Empty<string>(),
                Part2aAuthorizers = request.Part2aAuthorizers ?? Array.Empty<string>()
            }
        }, ct);

        return new CreatePurchaseRequestResponse(pr.PurchaseRequestId, exec);
    }

    public async Task<PurchaseRequestResponse> Get(string purchaseRequestId, CancellationToken ct)
    {
        var pr = await repo.Get(purchaseRequestId, ct);
        if (pr is null) throw new NotFoundException($"PurchaseRequest not found: {purchaseRequestId}");
        return new PurchaseRequestResponse(
            pr.PurchaseRequestId,
            pr.SpendAmount,
            pr.ContractValue,
            pr.CurrentAssignee,
            pr.ApprovalStatus.ToString(),
            pr.CreatedBy,
            pr.CreatedAtUtc);
    }
}

