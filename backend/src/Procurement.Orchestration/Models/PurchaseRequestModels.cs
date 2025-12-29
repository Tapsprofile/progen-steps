using Procurement.Orchestration.Core.Workflow;

namespace Procurement.Orchestration.Models;

public sealed record CreatePurchaseRequestRequest(
    string? PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string? CreatedBy,
    string[]? Part1dAuthorizers,
    string[]? Part2aAuthorizers);

public sealed record CreatePurchaseRequestResponse(
    string PurchaseRequestId,
    WorkflowExecutionView Execution);

public sealed record PurchaseRequestResponse(
    string PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string CurrentAssignee,
    string ApprovalStatus,
    string CreatedBy,
    DateTimeOffset CreatedAtUtc);

