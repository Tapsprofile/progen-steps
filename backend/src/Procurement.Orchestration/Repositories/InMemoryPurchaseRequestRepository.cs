using System.Collections.Concurrent;
using Procurement.Orchestration.Core.Domain;

namespace Procurement.Orchestration.Repositories;

public sealed class InMemoryPurchaseRequestRepository : IPurchaseRequestRepository
{
    private readonly ConcurrentDictionary<string, PurchaseRequest> _store = new();

    public Task Upsert(PurchaseRequest request, CancellationToken ct)
    {
        _store[request.PurchaseRequestId] = request;
        return Task.CompletedTask;
    }

    public Task<PurchaseRequest?> Get(string purchaseRequestId, CancellationToken ct)
        => Task.FromResult(_store.TryGetValue(purchaseRequestId, out var pr) ? pr : null);
}

