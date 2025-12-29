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

app.MapPost("/callbacks/bid-evaluation", async (SubmitBidEvaluationRequest req, IDispatcher dispatcher, CancellationToken ct) =>
{
    var result = await dispatcher.Send(new SubmitCallbackCommand(
        PurchaseRequestId: req.PurchaseRequestId,
        TokenType: TaskTokenType.BidEvaluationFormH,
        Output: new { FormType = "BID_EVALUATION_FORM_H", SubmittedAtUtc = DateTimeOffset.UtcNow, req }), ct);

    return result.Completed ? Results.Ok(result) : Results.NotFound(new { Message = "No task token found (workflow not waiting or token not persisted yet).", result });
}).WithOpenApi();

app.MapPost("/callbacks/document", async (SubmitDocumentRequest req, IDispatcher dispatcher, CancellationToken ct) =>
{
    var tokenType = req.DocumentType switch
    {
        "CABINET_APPROVAL" => TaskTokenType.CabinetApprovalDocument,
        "CABINET_REPORT" => TaskTokenType.CabinetReportDocument,
        _ => throw new ArgumentOutOfRangeException(nameof(req.DocumentType), "Unsupported DocumentType.")
    };

    var result = await dispatcher.Send(new SubmitCallbackCommand(
        PurchaseRequestId: req.PurchaseRequestId,
        TokenType: tokenType,
        Output: new { req.DocumentType, req.DocumentUri, SubmittedAtUtc = DateTimeOffset.UtcNow }), ct);

    return result.Completed ? Results.Ok(result) : Results.NotFound(new { Message = "No task token found.", result });
}).WithOpenApi();

app.MapPost("/callbacks/approval", async (SubmitApprovalRequest req, IDispatcher dispatcher, CancellationToken ct) =>
{
    var tokenType = req.Stage switch
    {
        "PART_1D" => TaskTokenType.Part1dApproval,
        "PART_2A" => TaskTokenType.Part2aApproval,
        _ => throw new ArgumentOutOfRangeException(nameof(req.Stage), "Unsupported stage.")
    };

    var result = await dispatcher.Send(new SubmitCallbackCommand(
        PurchaseRequestId: req.PurchaseRequestId,
        TokenType: tokenType,
        Output: new { req.Stage, req.AuthorizerId, req.Decision, SubmittedAtUtc = DateTimeOffset.UtcNow }), ct);

    return result.Completed ? Results.Ok(result) : Results.NotFound(new { Message = "No task token found.", result });
}).WithOpenApi();

// Local/dev helper: allow seeding a token to test callbacks without deploying Lambda requesters.
app.MapPost("/debug/task-tokens", async (SeedTaskTokenRequest req, ITaskTokenStore store, CancellationToken ct) =>
{
    await store.SaveToken(req.PurchaseRequestId, req.TokenType, req.TaskToken, ct);
    return Results.Ok(new { req.PurchaseRequestId, req.TokenType });
}).WithOpenApi();

app.Run();

internal sealed record CreatePurchaseRequestRequest(
    string? PurchaseRequestId,
    decimal SpendAmount,
    decimal ContractValue,
    string? CreatedBy,
    string[]? Part1dAuthorizers,
    string[]? Part2aAuthorizers);

internal sealed record SubmitBidEvaluationRequest(string PurchaseRequestId, string? Notes, decimal? AwardAmount);
internal sealed record SubmitDocumentRequest(string PurchaseRequestId, string DocumentType, string DocumentUri);
internal sealed record SubmitApprovalRequest(string PurchaseRequestId, string Stage, string AuthorizerId, string Decision);
internal sealed record SeedTaskTokenRequest(string PurchaseRequestId, TaskTokenType TokenType, string TaskToken);
