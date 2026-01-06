using System.Collections.Concurrent;
using Procurement.Orchestration.Application;

namespace Procurement.Orchestration.Infrastructure;

/// <summary>
/// In-memory CQRS read model for workflows. Replace with a DB-backed store in production.
/// </summary>
public sealed class InMemoryWorkflowStore : IWorkflowStore
{
    private readonly ConcurrentDictionary<string, WorkflowExecution> _executions = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<WorkflowStep>> _steps = new();
    private readonly ConcurrentDictionary<string, PendingTask> _pendingTasks = new(); // by TaskToken
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PendingTask>> _pendingByExecution = new(); // execId -> (token->task)
    private readonly ConcurrentDictionary<string, string> _taskTokenToExecution = new();

    public Task CreateExecution(WorkflowExecution execution, CancellationToken ct)
    {
        if (!_executions.TryAdd(execution.ExecutionId, execution))
            throw new InvalidOperationException($"Execution already exists: {execution.ExecutionId}");

        _steps.TryAdd(execution.ExecutionId, new ConcurrentQueue<WorkflowStep>());
        _pendingByExecution.TryAdd(execution.ExecutionId, new ConcurrentDictionary<string, PendingTask>());
        return Task.CompletedTask;
    }

    public Task UpdateExecution(WorkflowExecution execution, CancellationToken ct)
    {
        _executions[execution.ExecutionId] = execution;
        return Task.CompletedTask;
    }

    public Task AppendStep(WorkflowStep step, CancellationToken ct)
    {
        var q = _steps.GetOrAdd(step.ExecutionId, _ => new ConcurrentQueue<WorkflowStep>());
        q.Enqueue(step);
        return Task.CompletedTask;
    }

    public Task UpsertPendingTask(string executionId, PendingTask task, CancellationToken ct)
    {
        _pendingTasks[task.TaskToken] = task;
        _taskTokenToExecution[task.TaskToken] = executionId;

        var dict = _pendingByExecution.GetOrAdd(executionId, _ => new ConcurrentDictionary<string, PendingTask>());
        dict[task.TaskToken] = task;
        return Task.CompletedTask;
    }

    public Task CompletePendingTask(string taskToken, object callbackOutput, CancellationToken ct)
    {
        if (_taskTokenToExecution.TryRemove(taskToken, out var execId))
        {
            if (_pendingByExecution.TryGetValue(execId, out var dict))
            {
                dict.TryRemove(taskToken, out _);
            }
        }

        _pendingTasks.TryRemove(taskToken, out _);
        return Task.CompletedTask;
    }

    public Task<WorkflowExecution?> GetExecution(string executionId, CancellationToken ct)
        => Task.FromResult(_executions.TryGetValue(executionId, out var e) ? e : null);

    public Task<IReadOnlyList<WorkflowStep>> ListSteps(string executionId, CancellationToken ct)
    {
        if (!_steps.TryGetValue(executionId, out var q)) return Task.FromResult<IReadOnlyList<WorkflowStep>>([]);
        return Task.FromResult<IReadOnlyList<WorkflowStep>>(q.ToList());
    }

    public Task<IReadOnlyList<PendingTask>> ListPendingTasksByExecution(string executionId, CancellationToken ct)
    {
        if (!_pendingByExecution.TryGetValue(executionId, out var dict))
            return Task.FromResult<IReadOnlyList<PendingTask>>([]);

        var list = dict.Values.OrderBy(t => t.CreatedAtUtc).ToList();
        return Task.FromResult<IReadOnlyList<PendingTask>>(list);
    }

    public Task<(string ExecutionId, string PurchaseRequestId)?> FindExecutionByTaskToken(string taskToken, CancellationToken ct)
    {
        if (!_taskTokenToExecution.TryGetValue(taskToken, out var execId))
            return Task.FromResult<(string, string)?>(null);

        if (!_executions.TryGetValue(execId, out var exec))
            return Task.FromResult<(string, string)?>(null);

        return Task.FromResult<(string, string)?>(new(exec.ExecutionId, exec.PurchaseRequestId));
    }

    public Task<IReadOnlyList<PendingTask>> ListPendingTasksByPurchaseRequest(string purchaseRequestId, CancellationToken ct)
    {
        var list = _executions.Values
            .Where(e => e.PurchaseRequestId == purchaseRequestId)
            .SelectMany(e => _pendingByExecution.TryGetValue(e.ExecutionId, out var dict) ? dict.Values : [])
            .OrderBy(t => t.CreatedAtUtc)
            .ToList();

        return Task.FromResult<IReadOnlyList<PendingTask>>(list);
    }
}

