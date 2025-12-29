using Procurement.Orchestration.Application;
using Procurement.Orchestration.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(o =>
{
    o.AddDefaultPolicy(p => p
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

builder.Services
    .AddProcurementOrchestrationApplication()
    .AddProcurementOrchestrationInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/purchase-requests", async (CreatePurchaseRequestRequest req, IDispatcher dispatcher, CancellationToken ct) =>
{
    var id = string.IsNullOrWhiteSpace(req.PurchaseRequestId)
        ? $"PR-{DateTimeOffset.UtcNow:yyyy}-{Guid.NewGuid():N}".ToUpperInvariant()
        : req.PurchaseRequestId;

    var result = await dispatcher.Send(new CreatePurchaseRequestCommand(
        PurchaseRequestId: id,
        SpendAmount: req.SpendAmount,
        ContractValue: req.ContractValue,
        CreatedBy: string.IsNullOrWhiteSpace(req.CreatedBy) ? "REQUESTOR" : req.CreatedBy,
        Part1dAuthorizers: req.Part1dAuthorizers ?? Array.Empty<string>(),
        Part2aAuthorizers: req.Part2aAuthorizers ?? Array.Empty<string>()), ct);

    return Results.Ok(result);
}).WithOpenApi();

app.MapGet("/purchase-requests/{purchaseRequestId}", async (string purchaseRequestId, IDispatcher dispatcher, CancellationToken ct) =>
{
    var pr = await dispatcher.Query(new GetPurchaseRequestQuery(purchaseRequestId), ct);
    return pr is null ? Results.NotFound() : Results.Ok(pr);
}).WithOpenApi();

app.MapGet("/purchase-requests/{purchaseRequestId}/pending-tasks", async (string purchaseRequestId, IWorkflowRuntime workflow, CancellationToken ct) =>
{
    var tasks = await workflow.ListPendingTasks(purchaseRequestId, ct);
    return Results.Ok(tasks);
}).WithOpenApi();

app.MapPost("/workflow/callbacks", async (SubmitCallbackRequest req, IDispatcher dispatcher, CancellationToken ct) =>
{
    var result = await dispatcher.Send(new SubmitCallbackByTokenCommand(
        TaskToken: req.TaskToken,
        Output: new { SubmittedAtUtc = DateTimeOffset.UtcNow, req.Output }), ct);

    return Results.Ok(result);
}).WithOpenApi();

app.Run();

internal sealed record CreatePurchaseRequestRequest(
    string? PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string? CreatedBy,
    string[]? Part1dAuthorizers,
    string[]? Part2aAuthorizers);

internal sealed record SubmitCallbackRequest(string TaskToken, object Output);
