using Procurement.Orchestration.Core.Exceptions;

namespace Procurement.Orchestration.Core.Domain;

public enum PurchaseRequestStatus
{
    Draft = 0,
    Submitted = 1,
    InReview = 2,
    AwaitingDocuments = 3,
    AwaitingBidEvaluation = 4,
    ProcurementInProgress = 5,
    AwaitingAuthorisationPart1d = 6,
    AwaitingAuthorisationPart2a = 7,
    Approved = 8,
    Rejected = 9,
    Failed = 10
}

public sealed class PurchaseRequest
{
    private PurchaseRequest(
        string purchaseRequestId,
        decimal spendAmount,
        decimal contractValue,
        string createdBy,
        DateTimeOffset createdAtUtc)
    {
        PurchaseRequestId = purchaseRequestId;
        SpendAmount = spendAmount;
        ContractValue = contractValue;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public string PurchaseRequestId { get; }
    public decimal SpendAmount { get; }
    public decimal ContractValue { get; }

    // Query-friendly fields (explicitly requested)
    public string CurrentAssignee { get; private set; } = "REQUESTOR";
    public PurchaseRequestStatus ApprovalStatus { get; private set; } = PurchaseRequestStatus.Submitted;

    public string CreatedBy { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static PurchaseRequest CreateAndSubmit(string purchaseRequestId, decimal spendAmount, decimal contractValue, string createdBy)
    {
        if (string.IsNullOrWhiteSpace(purchaseRequestId))
            throw new ValidationException("PurchaseRequestId is required.");
        if (spendAmount <= 0)
            throw new ValidationException("SpendAmount must be > 0.");
        if (contractValue <= 0)
            throw new ValidationException("ContractValue must be > 0.");
        if (string.IsNullOrWhiteSpace(createdBy))
            throw new ValidationException("CreatedBy is required.");

        return new PurchaseRequest(purchaseRequestId, spendAmount, contractValue, createdBy, DateTimeOffset.UtcNow);
    }

    public void SetAssigneeAndStatus(string assignee, PurchaseRequestStatus status)
    {
        if (string.IsNullOrWhiteSpace(assignee))
            throw new ValidationException("Assignee is required.");

        CurrentAssignee = assignee;
        ApprovalStatus = status;
    }
}

