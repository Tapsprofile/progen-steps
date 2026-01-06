namespace Procurement.Orchestration.Application;

public sealed record SubmitCallbackByTokenCommand(string TaskToken, object Output)
    : ICommand<WorkflowResumeResult>;

public sealed class SubmitCallbackByTokenHandler(IWorkflowRuntime workflowRuntime)
    : ICommandHandler<SubmitCallbackByTokenCommand, WorkflowResumeResult>
{
    public Task<WorkflowResumeResult> Handle(SubmitCallbackByTokenCommand command, CancellationToken ct)
        => workflowRuntime.ResumeByTaskToken(command.TaskToken, command.Output, ct);
}

