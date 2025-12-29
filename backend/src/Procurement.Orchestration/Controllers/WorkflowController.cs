using Microsoft.AspNetCore.Mvc;
using Procurement.Orchestration.Core.Exceptions;
using Procurement.Orchestration.Core.Workflow;
using Procurement.Orchestration.Models;
using Procurement.Orchestration.Services;

namespace Procurement.Orchestration.Controllers;

[ApiController]
[Route("workflow")]
public sealed class WorkflowController(IWorkflowRuntime workflow) : ControllerBase
{
    [HttpGet("executions/{executionId}")]
    public async Task<ActionResult<WorkflowExecutionView>> GetExecution([FromRoute] string executionId, CancellationToken ct)
    {
        var view = await workflow.GetExecution(executionId, ct);
        if (view is null) throw new NotFoundException($"Execution not found: {executionId}");
        return Ok(view);
    }

    [HttpPost("callbacks")]
    public async Task<ActionResult<WorkflowExecutionView>> Callback([FromBody] SubmitCallbackRequest request, CancellationToken ct)
        => Ok(await workflow.Resume(request.TaskToken, new { SubmittedAtUtc = DateTimeOffset.UtcNow, request.Output }, ct));
}

