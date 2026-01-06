using Microsoft.AspNetCore.Mvc;
using Procurement.Orchestration.Models;
using Procurement.Orchestration.Services;

namespace Procurement.Orchestration.Controllers;

[ApiController]
[Route("purchase-requests")]
public sealed class PurchaseRequestsController(IPurchaseRequestService service, IWorkflowRuntime workflow) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreatePurchaseRequestResponse>> Create([FromBody] CreatePurchaseRequestRequest request, CancellationToken ct)
        => Ok(await service.CreateAndStart(request, ct));

    [HttpGet("{purchaseRequestId}")]
    public async Task<ActionResult<PurchaseRequestResponse>> Get([FromRoute] string purchaseRequestId, CancellationToken ct)
        => Ok(await service.Get(purchaseRequestId, ct));

    [HttpGet("{purchaseRequestId}/pending-tasks")]
    public async Task<ActionResult<IReadOnlyList<Core.Workflow.PendingTask>>> PendingTasks([FromRoute] string purchaseRequestId, CancellationToken ct)
        => Ok(await workflow.ListPendingTasks(purchaseRequestId, ct));
}

