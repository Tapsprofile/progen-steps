using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Procurement.Orchestration.Application;

namespace Procurement.Orchestration.Infrastructure;

public sealed class HandwrittenWorkflowOptions
{
    /// <summary>
    /// Path to the handwritten workflow JSON definition.
    /// </summary>
    public string DefinitionPath { get; init; } = "/workspace/workflow/handwritten/purchase-request-workflow.json";
}

internal sealed record ExecutionState(
    string ExecutionId,
    string PurchaseRequestId,
    WorkflowExecutionStatus Status,
    int PhaseIndex,
    JsonObject Data,
    List<PendingTask> PendingTasks);

/// <summary>
/// Handwritten (non-AWS) orchestration runtime.
/// Implements the required procurement workflow as a deterministic phase machine:
/// - spend routing
/// - optional cabinet dual-doc wait
/// - Form H wait
/// - Module I tasks
/// - Part 1d + Part 2a authorisation maps (N tokens)
/// </summary>
public sealed class HandwrittenWorkflowRuntime : IWorkflowRuntime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ILogger<HandwrittenWorkflowRuntime> _log;
    private readonly HandwrittenWorkflowOptions _options;

    // In-memory store for demo; replace with DB/event store for production.
    private readonly ConcurrentDictionary<string, ExecutionState> _executions = new();
    private readonly ConcurrentDictionary<string, string> _taskTokenToExecutionId = new();

    private HandwrittenWorkflowDefinition? _definition;

    public HandwrittenWorkflowRuntime(ILogger<HandwrittenWorkflowRuntime> log, IOptions<HandwrittenWorkflowOptions> options)
    {
        _log = log;
        _options = options.Value;
    }

    public Task<IReadOnlyList<PendingTask>> ListPendingTasks(string purchaseRequestId, CancellationToken ct)
    {
        var tokens = _executions.Values
            .Where(e => e.PurchaseRequestId == purchaseRequestId)
            .SelectMany(e => e.PendingTasks)
            .OrderBy(t => t.CreatedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<PendingTask>>(tokens);
    }

    public Task<WorkflowStartResult> StartPurchaseRequestWorkflow(object input, CancellationToken ct)
    {
        EnsureLoaded();

        var data = JsonSerializer.SerializeToNode(input, Json)?.AsObject()
                   ?? throw new InvalidOperationException("Invalid input: expected JSON object.");

        var purchaseRequestId = data["PurchaseRequestId"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(purchaseRequestId))
            throw new InvalidOperationException("Input must include PurchaseRequestId.");

        var executionId = $"exec_{Guid.NewGuid():N}";
        var state = new ExecutionState(
            ExecutionId: executionId,
            PurchaseRequestId: purchaseRequestId,
            Status: WorkflowExecutionStatus.Running,
            PhaseIndex: 0,
            Data: data,
            PendingTasks: new List<PendingTask>());

        state = RunUntilWaitOrEnd(state);
        _executions[executionId] = state;

        return Task.FromResult(new WorkflowStartResult(
            ExecutionId: state.ExecutionId,
            PurchaseRequestId: state.PurchaseRequestId,
            Status: state.Status,
            PendingTasks: state.PendingTasks.ToList()));
    }

    public Task<WorkflowResumeResult> ResumeByTaskToken(string taskToken, object output, CancellationToken ct)
    {
        if (!_taskTokenToExecutionId.TryGetValue(taskToken, out var executionId) || !_executions.TryGetValue(executionId, out var state))
            throw new InvalidOperationException("Unknown task token.");

        // Mark token complete + attach callback output under $.Callbacks[taskToken]
        state.Data["Callbacks"] ??= new JsonObject();
        state.Data["Callbacks"]!.AsObject()[taskToken] = JsonSerializer.SerializeToNode(output, Json);

        state.PendingTasks.RemoveAll(t => t.TaskToken == taskToken);
        _taskTokenToExecutionId.TryRemove(taskToken, out _);

        // If we were waiting and no pending tasks remain for the current phase, continue.
        if (state.Status == WorkflowExecutionStatus.Waiting && state.PendingTasks.Count == 0)
        {
            state = state with { Status = WorkflowExecutionStatus.Running };
        }

        state = RunUntilWaitOrEnd(state);
        _executions[executionId] = state;

        return Task.FromResult(new WorkflowResumeResult(
            ExecutionId: state.ExecutionId,
            PurchaseRequestId: state.PurchaseRequestId,
            Status: state.Status,
            PendingTasks: state.PendingTasks.ToList()));
    }

    private ExecutionState RunUntilWaitOrEnd(ExecutionState state)
    {
        EnsureLoaded();

        try
        {
            while (state.Status == WorkflowExecutionStatus.Running)
            {
                if (state.PhaseIndex >= _definition!.Phases.Count)
                {
                    return state with { Status = WorkflowExecutionStatus.Succeeded };
                }

                var phase = _definition.Phases[state.PhaseIndex];
                state = phase.Type switch
                {
                    "routeBySpend" => ExecuteRouteBySpend(state),
                    "cabinetDocsIfRequired" => ExecuteCabinetDocsIfRequired(state, phase),
                    "waitForFormH" => ExecuteWaitForFormH(state, phase),
                    "procurementModuleI" => ExecuteProcurementModuleI(state, phase),
                    "authorisationMap" => ExecuteAuthorisationMap(state, phase),
                    "succeed" => state with { Status = WorkflowExecutionStatus.Succeeded, PhaseIndex = state.PhaseIndex + 1 },
                    _ => throw new InvalidOperationException($"Unsupported phase type: {phase.Type}")
                };
            }

            return state;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Workflow execution failed. ExecutionId={ExecutionId} PurchaseRequestId={PurchaseRequestId}", state.ExecutionId, state.PurchaseRequestId);
            return state with { Status = WorkflowExecutionStatus.Failed };
        }
    }

    private ExecutionState ExecuteRouteBySpend(ExecutionState state)
    {
        var spendAmount = state.Data["SpendAmount"]?.GetValue<decimal>() ?? 0m;

        // Persist "CurrentAssignee" and "ApprovalStatus" as per prompt suggestions.
        state.Data["ApprovalStatus"] = "IN_REVIEW";

        if (spendAmount >= _definition!.Thresholds.CabinetApproval)
        {
            state.Data["CurrentAssignee"] = "CABINET_OFFICE";
            state.Data["RoutingLevel"] = "LEVEL_5_CABINET_APPROVAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spendAmount >= _definition.Thresholds.HighValueLegal)
        {
            state.Data["CurrentAssignee"] = "LEGAL_MONITORING_OFFICER_HIGH_VALUE";
            state.Data["RoutingLevel"] = "LEVEL_4_HIGH_VALUE_LEGAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spendAmount > _definition.Thresholds.FinanceAndLegal)
        {
            state.Data["CurrentAssignee"] = "FINANCE_AND_LEGAL";
            state.Data["RoutingLevel"] = "LEVEL_3_FINANCE_AND_LEGAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spendAmount > _definition.Thresholds.TeamManagerLimit)
        {
            state.Data["CurrentAssignee"] = "HEAD_OF_SERVICE";
            state.Data["RoutingLevel"] = "LEVEL_2_HEAD_OF_SERVICE";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        state.Data["CurrentAssignee"] = "TEAM_MANAGER";
        state.Data["RoutingLevel"] = "LEVEL_1_TEAM_MANAGER";
        return state with { PhaseIndex = state.PhaseIndex + 1 };
    }

    private ExecutionState ExecuteCabinetDocsIfRequired(ExecutionState state, HandwrittenWorkflowPhase phase)
    {
        var routingLevel = state.Data["RoutingLevel"]?.GetValue<string>();
        if (!string.Equals(routingLevel, "LEVEL_5_CABINET_APPROVAL", StringComparison.OrdinalIgnoreCase))
        {
            // Skip cabinet docs phase.
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (state.PendingTasks.Count > 0)
        {
            return state with { Status = WorkflowExecutionStatus.Waiting };
        }

        foreach (var doc in phase.Documents)
        {
            var token = NewToken(state, taskType: "DOCUMENT_UPLOAD", actorId: null, $"Upload required document: {doc}", extra: new JsonObject { ["DocumentType"] = doc });
            state.PendingTasks.Add(token);
            _taskTokenToExecutionId[token.TaskToken] = state.ExecutionId;
        }

        state.Data["ApprovalStatus"] = "AWAITING_DOCUMENTS";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private ExecutionState ExecuteWaitForFormH(ExecutionState state, HandwrittenWorkflowPhase phase)
    {
        if (state.PendingTasks.Count > 0)
        {
            return state with { Status = WorkflowExecutionStatus.Waiting };
        }

        var token = NewToken(state, taskType: "FORM_H", actorId: null, $"Submit {phase.Form}", extra: new JsonObject { ["FormType"] = phase.Form });
        state.PendingTasks.Add(token);
        _taskTokenToExecutionId[token.TaskToken] = state.ExecutionId;

        state.Data["ApprovalStatus"] = "AWAITING_BID_EVALUATION";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private ExecutionState ExecuteProcurementModuleI(ExecutionState state, HandwrittenWorkflowPhase phase)
    {
        var done = state.Data["ModuleI"] as JsonObject ?? new JsonObject();
        foreach (var t in phase.Tasks)
        {
            done[t] = true;
        }

        state.Data["ModuleI"] = done;
        state.Data["ApprovalStatus"] = "PROCUREMENT_IN_PROGRESS";
        return state with { PhaseIndex = state.PhaseIndex + 1 };
    }

    private ExecutionState ExecuteAuthorisationMap(ExecutionState state, HandwrittenWorkflowPhase phase)
    {
        if (state.PendingTasks.Count > 0)
        {
            return state with { Status = WorkflowExecutionStatus.Waiting };
        }

        var authorizers = ResolveStringArray(state.Data, phase.AuthorizersPath);
        foreach (var authId in authorizers)
        {
            var token = NewToken(
                state,
                taskType: $"APPROVAL_{phase.Stage}",
                actorId: authId,
                description: $"Approval required: {phase.Stage} by {authId}",
                extra: new JsonObject { ["Stage"] = phase.Stage, ["AuthorizerId"] = authId });

            state.PendingTasks.Add(token);
            _taskTokenToExecutionId[token.TaskToken] = state.ExecutionId;
        }

        state.Data["ApprovalStatus"] = phase.Stage == "PART_1D" ? "AWAITING_AUTHORISATION_PART_1D" : "AWAITING_AUTHORISATION_PART_2A";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private static string[] ResolveStringArray(JsonObject root, string jsonPath)
    {
        // Supports only the paths we use: $.Authorization.Part1dAuthorizers / $.Authorization.Part2aAuthorizers
        var tokens = jsonPath.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length < 2 || tokens[0] != "$") return Array.Empty<string>();

        JsonNode? node = root;
        for (var i = 1; i < tokens.Length; i++)
        {
            node = node is JsonObject obj && obj.TryGetPropertyValue(tokens[i], out var child) ? child : null;
            if (node is null) return Array.Empty<string>();
        }

        if (node is not JsonArray arr) return Array.Empty<string>();
        return arr.Select(v => v?.GetValue<string>()).Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToArray();
    }

    private static PendingTask NewToken(ExecutionState state, string taskType, string? actorId, string description, JsonObject? extra)
    {
        var token = $"t_{Guid.NewGuid():N}";

        // Persist token info under workflow data as suggested schema improvement.
        state.Data["TaskTokens"] ??= new JsonArray();
        state.Data["TaskTokens"]!.AsArray().Add(new JsonObject
        {
            ["TaskToken"] = token,
            ["TaskType"] = taskType,
            ["ActorId"] = actorId,
            ["Description"] = description,
            ["CreatedAtUtc"] = DateTimeOffset.UtcNow.ToString("O"),
            ["Extra"] = extra
        });

        return new PendingTask(
            TaskToken: token,
            PurchaseRequestId: state.PurchaseRequestId,
            TaskType: taskType,
            ActorId: actorId,
            Description: description,
            CreatedAtUtc: DateTimeOffset.UtcNow);
    }

    private void EnsureLoaded()
    {
        if (_definition is not null) return;
        var json = File.ReadAllText(_options.DefinitionPath);
        _definition = JsonSerializer.Deserialize<HandwrittenWorkflowDefinition>(json, Json)
                      ?? throw new InvalidOperationException("Failed to load workflow definition.");
    }
}

internal sealed class HandwrittenWorkflowDefinition
{
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public string Description { get; set; } = "";
    public HandwrittenThresholds Thresholds { get; set; } = new();
    public List<HandwrittenWorkflowPhase> Phases { get; set; } = new();
}

internal sealed class HandwrittenThresholds
{
    public decimal SpendAuthLimit { get; set; }
    public decimal TeamManagerLimit { get; set; }
    public decimal FinanceAndLegal { get; set; }
    public decimal HighValueLegal { get; set; }
    public decimal CabinetApproval { get; set; }
}

internal sealed class HandwrittenWorkflowPhase
{
    public string Type { get; set; } = "";
    public List<string> Documents { get; set; } = new();
    public string Form { get; set; } = "";
    public List<string> Tasks { get; set; } = new();
    public string Stage { get; set; } = "";
    public string AuthorizersPath { get; set; } = "";
}

