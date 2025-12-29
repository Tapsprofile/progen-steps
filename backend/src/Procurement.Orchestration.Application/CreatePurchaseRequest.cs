using Procurement.Orchestration.Domain;

namespace Procurement.Orchestration.Application;

public sealed record CreatePurchaseRequestCommand(
    string PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string CreatedBy,
    string[] Part1dAuthorizers,
    string[] Part2aAuthorizers) : ICommand<CreatePurchaseRequestResult>;

public sealed record CreatePurchaseRequestResult(string PurchaseRequestId, string WorkflowExecutionArn);

public sealed class CreatePurchaseRequestHandler(
    IPurchaseRequestRepository repository,
    IEventPublisher events,
    IWorkflowStarter workflowStarter)
    : ICommandHandler<CreatePurchaseRequestCommand, CreatePurchaseRequestResult>
{
    public async Task<CreatePurchaseRequestResult> Handle(CreatePurchaseRequestCommand command, CancellationToken ct)
    {
        var pr = PurchaseRequest.CreateAndSubmit(
            command.PurchaseRequestId,
            command.SpendAmount,
            command.ContractValue,
            createdBy: command.CreatedBy);

        // Persist query-friendly fields (CQRS read model is typically separate; this is a minimal starter).
        await repository.Upsert(pr, ct);

        // Event module (Event-Driven): publish "Purchase Request Created".
        foreach (var e in pr.DequeueEvents())
        {
            await events.Publish(e, ct);
        }

        // Workflow module: start Step Functions execution.
        var executionArn = await workflowStarter.StartPurchaseRequestWorkflow(
            purchaseRequestId: pr.PurchaseRequestId,
            spendAmount: pr.SpendAmount,
            contractValue: pr.ContractValue,
            currentAssignee: pr.CurrentAssignee,
            approvalStatus: pr.Status.ToString(),
            part1dAuthorizers: command.Part1dAuthorizers,
            part2aAuthorizers: command.Part2aAuthorizers,
            ct);

        return new CreatePurchaseRequestResult(pr.PurchaseRequestId, executionArn);
    }
}

