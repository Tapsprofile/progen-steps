namespace Procurement.Orchestration.Application;

public sealed record GetWorkflowExecutionViewQuery(string ExecutionId) : IQuery<WorkflowExecutionView?>;

public sealed class GetWorkflowExecutionViewHandler(IWorkflowRuntime workflow)
    : IQueryHandler<GetWorkflowExecutionViewQuery, WorkflowExecutionView?>
{
    public Task<WorkflowExecutionView?> Handle(GetWorkflowExecutionViewQuery query, CancellationToken ct)
        => workflow.GetExecutionView(query.ExecutionId, ct);
}

