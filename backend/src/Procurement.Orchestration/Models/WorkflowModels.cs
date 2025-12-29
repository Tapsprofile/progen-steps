using Procurement.Orchestration.Core.Workflow;

namespace Procurement.Orchestration.Models;

public sealed record SubmitCallbackRequest(string TaskToken, object Output);

public sealed record WorkflowExecutionResponse(WorkflowExecutionView View);

