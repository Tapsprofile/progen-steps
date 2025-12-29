using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using Procurement.Orchestration.Core.Workflow;
using Procurement.Orchestration.Repositories;

namespace Procurement.Orchestration.Services;

public sealed class HandwrittenWorkflowOptions
{
    public string DefinitionPath { get; init; } = "/workspace/workflow/handwritten/purchase-request-workflow.json";
}

public interface IWorkflowRuntime
{
    Task<WorkflowExecutionView> Start(object input, CancellationToken ct);
    Task<WorkflowExecutionView> Resume(string taskToken, object output, CancellationToken ct);
    Task<WorkflowExecutionView?> GetExecution(string executionId, CancellationToken ct);
    Task<IReadOnlyList<PendingTask>> ListPendingTasks(string purchaseRequestId, CancellationToken ct);
}

internal sealed record ExecutionState(
    string ExecutionId,
    string PurchaseRequestId,
    WorkflowExecutionStatus Status,
    int PhaseIndex,
    JsonObject Data,
    List<PendingTask> PendingTasks);

public sealed class HandwrittenWorkflowRuntime : IWorkflowRuntime
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ILogger<HandwrittenWorkflowRuntime> _log;
    private readonly HandwrittenWorkflowOptions _options;
    private readonly IWorkflowStore _store;

    private readonly Dictionary<string, ExecutionState> _cache = new();
    private Definition? _definition;

    public HandwrittenWorkflowRuntime(
        ILogger<HandwrittenWorkflowRuntime> log,
        IOptions<HandwrittenWorkflowOptions> options,
        IWorkflowStore store)
    {
        _log = log;
        _options = options.Value;
        _store = store;
    }

    public Task<IReadOnlyList<PendingTask>> ListPendingTasks(string purchaseRequestId, CancellationToken ct)
        => _store.ListPendingTasksByPurchaseRequest(purchaseRequestId, ct);

    public async Task<WorkflowExecutionView?> GetExecution(string executionId, CancellationToken ct)
    {
        var exec = await _store.GetExecution(executionId, ct);
        if (exec is null) return null;
        var steps = await _store.ListSteps(executionId, ct);
        var tasks = await _store.ListPendingTasksByExecution(executionId, ct);
        return new WorkflowExecutionView(exec, steps, tasks);
    }

    public async Task<WorkflowExecutionView> Start(object input, CancellationToken ct)
    {
        EnsureLoaded();
        var data = JsonSerializer.SerializeToNode(input, Json)?.AsObject() ?? throw new InvalidOperationException("Expected object input.");
        var prId = data["PurchaseRequestId"]?.GetValue<string>() ?? throw new InvalidOperationException("PurchaseRequestId required.");

        var executionId = $"exec_{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow;

        var exec = new WorkflowExecution(
            ExecutionId: executionId,
            PurchaseRequestId: prId,
            DefinitionName: _definition!.Name,
            DefinitionVersion: _definition.Version,
            Status: WorkflowExecutionStatus.Running,
            PhaseIndex: 0,
            Data: data,
            CreatedAtUtc: now,
            UpdatedAtUtc: now);

        await _store.CreateExecution(exec, ct);
        await _store.AppendStep(NewStep(executionId, prId, "workflow", WorkflowStepType.PhaseEnter, null, data, null, WorkflowStepStatus.Ok), ct);

        var state = new ExecutionState(executionId, prId, WorkflowExecutionStatus.Running, 0, data, new List<PendingTask>());
        state = await Run(state, ct);
        _cache[executionId] = state;

        return (await GetExecution(executionId, ct))!;
    }

    public async Task<WorkflowExecutionView> Resume(string taskToken, object output, CancellationToken ct)
    {
        var found = await _store.FindExecutionByTaskToken(taskToken, ct) ?? throw new InvalidOperationException("Unknown task token.");
        var executionId = found.ExecutionId;

        if (!_cache.TryGetValue(executionId, out var state))
        {
            var exec = await _store.GetExecution(executionId, ct) ?? throw new InvalidOperationException("Execution not found.");
            var data = JsonSerializer.SerializeToNode(exec.Data, Json)?.AsObject() ?? new JsonObject();
            state = new ExecutionState(exec.ExecutionId, exec.PurchaseRequestId, exec.Status, exec.PhaseIndex, data, new List<PendingTask>());
        }

        state.Data["Callbacks"] ??= new JsonObject();
        state.Data["Callbacks"]!.AsObject()[taskToken] = JsonSerializer.SerializeToNode(output, Json);

        await _store.CompletePendingTask(taskToken, output, ct);
        await _store.AppendStep(NewStep(executionId, state.PurchaseRequestId, "callback", WorkflowStepType.TokenCompleted, taskToken, null, output, WorkflowStepStatus.Ok), ct);

        // best-effort: continue
        state = state with { Status = WorkflowExecutionStatus.Running };
        state = await Run(state, ct);
        _cache[executionId] = state;

        return (await GetExecution(executionId, ct))!;
    }

    private async Task<ExecutionState> Run(ExecutionState state, CancellationToken ct)
    {
        EnsureLoaded();
        try
        {
            while (state.Status == WorkflowExecutionStatus.Running)
            {
                if (state.PhaseIndex >= _definition!.Phases.Count)
                {
                    state = state with { Status = WorkflowExecutionStatus.Succeeded };
                    await Persist(state, ct);
                    return state;
                }

                var phase = _definition.Phases[state.PhaseIndex];
                await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, phase.Type, WorkflowStepType.PhaseEnter, null, state.Data, null, WorkflowStepStatus.Ok), ct);

                state = phase.Type switch
                {
                    "routeBySpend" => RouteBySpend(state),
                    "procurementModuleI" => ProcurementModuleI(state, phase),
                    "cabinetDocsIfRequired" => await CabinetDocs(state, phase, ct),
                    "waitForFormH" => await WaitForFormH(state, phase, ct),
                    "authorisationMap" => await AuthorisationMap(state, phase, ct),
                    "succeed" => state with { Status = WorkflowExecutionStatus.Succeeded, PhaseIndex = state.PhaseIndex + 1 },
                    _ => throw new InvalidOperationException($"Unsupported phase: {phase.Type}")
                };

                await Persist(state, ct);
                await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, phase.Type, WorkflowStepType.PhaseComplete, null, null, state.Data, state.Status == WorkflowExecutionStatus.Waiting ? WorkflowStepStatus.Waiting : WorkflowStepStatus.Ok), ct);

                if (state.Status == WorkflowExecutionStatus.Waiting)
                    return state;
            }

            return state;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Workflow failed");
            await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, "error", WorkflowStepType.Error, null, null, new { ex.Message }, WorkflowStepStatus.Failed), ct);
            state = state with { Status = WorkflowExecutionStatus.Failed };
            await Persist(state, ct);
            return state;
        }
    }

    private ExecutionState RouteBySpend(ExecutionState state)
    {
        var spend = state.Data["SpendAmount"]?.GetValue<decimal>() ?? 0m;
        state.Data["ApprovalStatus"] = "IN_REVIEW";

        if (spend >= _definition!.Thresholds.CabinetApproval)
        {
            state.Data["CurrentAssignee"] = "CABINET_OFFICE";
            state.Data["RoutingLevel"] = "LEVEL_5_CABINET_APPROVAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spend >= _definition.Thresholds.HighValueLegal)
        {
            state.Data["CurrentAssignee"] = "LEGAL_MONITORING_OFFICER_HIGH_VALUE";
            state.Data["RoutingLevel"] = "LEVEL_4_HIGH_VALUE_LEGAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spend > _definition.Thresholds.FinanceAndLegal)
        {
            state.Data["CurrentAssignee"] = "FINANCE_AND_LEGAL";
            state.Data["RoutingLevel"] = "LEVEL_3_FINANCE_AND_LEGAL";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        if (spend > _definition.Thresholds.TeamManagerLimit)
        {
            state.Data["CurrentAssignee"] = "HEAD_OF_SERVICE";
            state.Data["RoutingLevel"] = "LEVEL_2_HEAD_OF_SERVICE";
            return state with { PhaseIndex = state.PhaseIndex + 1 };
        }

        state.Data["CurrentAssignee"] = "TEAM_MANAGER";
        state.Data["RoutingLevel"] = "LEVEL_1_TEAM_MANAGER";
        return state with { PhaseIndex = state.PhaseIndex + 1 };
    }

    private async Task<ExecutionState> CabinetDocs(ExecutionState state, Phase phase, CancellationToken ct)
    {
        if (!string.Equals(state.Data["RoutingLevel"]?.GetValue<string>(), "LEVEL_5_CABINET_APPROVAL", StringComparison.OrdinalIgnoreCase))
            return state with { PhaseIndex = state.PhaseIndex + 1 };

        foreach (var doc in phase.Documents)
        {
            var t = NewTask(state.PurchaseRequestId, "DOCUMENT_UPLOAD", null, $"Upload required document: {doc}");
            state.PendingTasks.Add(t);
            await _store.UpsertPendingTask(state.ExecutionId, t, ct);
            await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, "cabinetDocsIfRequired", WorkflowStepType.TokenIssued, t.TaskToken, new { doc }, t, WorkflowStepStatus.Waiting), ct);
        }

        state.Data["ApprovalStatus"] = "AWAITING_DOCUMENTS";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private async Task<ExecutionState> WaitForFormH(ExecutionState state, Phase phase, CancellationToken ct)
    {
        var t = NewTask(state.PurchaseRequestId, "FORM_H", null, $"Submit {phase.Form}");
        state.PendingTasks.Add(t);
        await _store.UpsertPendingTask(state.ExecutionId, t, ct);
        await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, "waitForFormH", WorkflowStepType.TokenIssued, t.TaskToken, new { phase.Form }, t, WorkflowStepStatus.Waiting), ct);

        state.Data["ApprovalStatus"] = "AWAITING_BID_EVALUATION";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private ExecutionState ProcurementModuleI(ExecutionState state, Phase phase)
    {
        var done = state.Data["ModuleI"] as JsonObject ?? new JsonObject();
        foreach (var t in phase.Tasks) done[t] = true;
        state.Data["ModuleI"] = done;
        state.Data["ApprovalStatus"] = "PROCUREMENT_IN_PROGRESS";
        return state with { PhaseIndex = state.PhaseIndex + 1 };
    }

    private async Task<ExecutionState> AuthorisationMap(ExecutionState state, Phase phase, CancellationToken ct)
    {
        var authorizers = ResolveStringArray(state.Data, phase.AuthorizersPath);
        foreach (var a in authorizers)
        {
            var t = NewTask(state.PurchaseRequestId, $"APPROVAL_{phase.Stage}", a, $"Approval required: {phase.Stage} by {a}");
            state.PendingTasks.Add(t);
            await _store.UpsertPendingTask(state.ExecutionId, t, ct);
            await _store.AppendStep(NewStep(state.ExecutionId, state.PurchaseRequestId, "authorisationMap", WorkflowStepType.TokenIssued, t.TaskToken, new { phase.Stage, a }, t, WorkflowStepStatus.Waiting), ct);
        }

        state.Data["ApprovalStatus"] = phase.Stage == "PART_1D" ? "AWAITING_AUTHORISATION_PART_1D" : "AWAITING_AUTHORISATION_PART_2A";
        return state with { Status = WorkflowExecutionStatus.Waiting };
    }

    private async Task Persist(ExecutionState state, CancellationToken ct)
    {
        EnsureLoaded();
        var now = DateTimeOffset.UtcNow;
        var existing = await _store.GetExecution(state.ExecutionId, ct);
        var createdAt = existing?.CreatedAtUtc ?? now;

        var exec = new WorkflowExecution(
            state.ExecutionId,
            state.PurchaseRequestId,
            _definition!.Name,
            _definition.Version,
            state.Status,
            state.PhaseIndex,
            state.Data,
            createdAt,
            now);

        await _store.UpdateExecution(exec, ct);
    }

    private static WorkflowStep NewStep(
        string executionId,
        string prId,
        string stepName,
        WorkflowStepType type,
        string? correlationId,
        object? input,
        object? output,
        WorkflowStepStatus status)
        => new($"step_{Guid.NewGuid():N}", executionId, prId, stepName, type, correlationId, input, output, status, DateTimeOffset.UtcNow);

    private static PendingTask NewTask(string prId, string taskType, string? actorId, string description)
        => new($"t_{Guid.NewGuid():N}", prId, taskType, actorId, description, DateTimeOffset.UtcNow);

    private static string[] ResolveStringArray(JsonObject root, string jsonPath)
    {
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

    private void EnsureLoaded()
    {
        if (_definition is not null) return;
        var json = File.ReadAllText(_options.DefinitionPath);
        _definition = JsonSerializer.Deserialize<Definition>(json, Json) ?? throw new InvalidOperationException("Invalid workflow definition.");
    }

    private sealed class Definition
    {
        public string Name { get; set; } = "";
        public int Version { get; set; }
        public Thresholds Thresholds { get; set; } = new();
        public List<Phase> Phases { get; set; } = new();
    }

    private sealed class Thresholds
    {
        public decimal SpendAuthLimit { get; set; }
        public decimal TeamManagerLimit { get; set; }
        public decimal FinanceAndLegal { get; set; }
        public decimal HighValueLegal { get; set; }
        public decimal CabinetApproval { get; set; }
    }

    private sealed class Phase
    {
        public string Type { get; set; } = "";
        public List<string> Documents { get; set; } = new();
        public string Form { get; set; } = "";
        public List<string> Tasks { get; set; } = new();
        public string Stage { get; set; } = "";
        public string AuthorizersPath { get; set; } = "";
    }
}

