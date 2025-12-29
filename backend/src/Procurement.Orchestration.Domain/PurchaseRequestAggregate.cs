using System.Collections.Immutable;

namespace Procurement.Orchestration.Domain;

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

/// <summary>
/// Minimal, explicit state-machine style aggregate.
/// The Step Functions workflow is the orchestrator; this domain model persists
/// "query-friendly" fields (CurrentAssignee/ApprovalStatus) and supports CQRS reads/writes.
/// </summary>
public sealed class PurchaseRequest
{
    private PurchaseRequest(
        string purchaseRequestId,
        decimal spendAmount,
        decimal contractValue,
        PurchaseRequestStatus status,
        string currentAssignee,
        ImmutableList<DomainEvent> pendingEvents)
    {
        PurchaseRequestId = purchaseRequestId;
        SpendAmount = spendAmount;
        ContractValue = contractValue;
        Status = status;
        CurrentAssignee = currentAssignee;
        PendingEvents = pendingEvents;
    }

    public string PurchaseRequestId { get; }
    public decimal SpendAmount { get; }
    public decimal ContractValue { get; }

    /// <summary>
    /// "ApprovalStatus" in the prompt maps to this persisted state.
    /// </summary>
    public PurchaseRequestStatus Status { get; private set; }

    /// <summary>
    /// Suggested improvement: persisted to support UI "bucket" views and audits.
    /// </summary>
    public string CurrentAssignee { get; private set; } = "UNASSIGNED";

    public ImmutableList<DomainEvent> PendingEvents { get; private set; } = ImmutableList<DomainEvent>.Empty;

    public static PurchaseRequest CreateAndSubmit(
        string purchaseRequestId,
        decimal spendAmount,
        decimal contractValue,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(purchaseRequestId)) throw new ArgumentException("PurchaseRequestId is required.", nameof(purchaseRequestId));
        if (spendAmount <= 0) throw new ArgumentOutOfRangeException(nameof(spendAmount), "SpendAmount must be > 0.");
        if (contractValue <= 0) throw new ArgumentOutOfRangeException(nameof(contractValue), "ContractValue must be > 0.");

        var pr = new PurchaseRequest(
            purchaseRequestId,
            spendAmount,
            contractValue,
            PurchaseRequestStatus.Submitted,
            currentAssignee: createdBy,
            pendingEvents: ImmutableList<DomainEvent>.Empty);

        pr.AddEvent(new PurchaseRequestCreated(purchaseRequestId, spendAmount, contractValue, createdBy));
        return pr;
    }

    public void AssignTo(string assignee)
    {
        if (string.IsNullOrWhiteSpace(assignee)) throw new ArgumentException("assignee is required.", nameof(assignee));
        CurrentAssignee = assignee;
        Status = PurchaseRequestStatus.InReview;
        AddEvent(new PurchaseRequestAssigned(PurchaseRequestId, assignee));
    }

    public void MarkAwaitingBidEvaluation()
    {
        Status = PurchaseRequestStatus.AwaitingBidEvaluation;
        AddEvent(new PurchaseRequestAwaitingBidEvaluation(PurchaseRequestId));
    }

    public void MarkFailed(string reason)
    {
        Status = PurchaseRequestStatus.Failed;
        AddEvent(new PurchaseRequestFailed(PurchaseRequestId, reason));
    }

    public ImmutableList<DomainEvent> DequeueEvents()
    {
        var events = PendingEvents;
        PendingEvents = ImmutableList<DomainEvent>.Empty;
        return events;
    }

    private void AddEvent(DomainEvent e) => PendingEvents = PendingEvents.Add(e);
}

public abstract record DomainEvent(string EventType, DateTimeOffset OccurredAtUtc);

public sealed record PurchaseRequestCreated(
    string PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string CreatedBy)
    : DomainEvent("PurchaseRequestCreated", DateTimeOffset.UtcNow);

public sealed record PurchaseRequestAssigned(
    string PurchaseRequestId,
    string Assignee)
    : DomainEvent("PurchaseRequestAssigned", DateTimeOffset.UtcNow);

public sealed record PurchaseRequestAwaitingBidEvaluation(string PurchaseRequestId)
    : DomainEvent("PurchaseRequestAwaitingBidEvaluation", DateTimeOffset.UtcNow);

public sealed record PurchaseRequestFailed(string PurchaseRequestId, string Reason)
    : DomainEvent("PurchaseRequestFailed", DateTimeOffset.UtcNow);

