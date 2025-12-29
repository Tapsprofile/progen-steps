using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Procurement.Orchestration.Application;
using Procurement.Orchestration.Domain;

namespace Procurement.Orchestration.Infrastructure;

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

public sealed class InMemoryEventPublisher(ILogger<InMemoryEventPublisher> log) : IEventPublisher
{
    public Task Publish(DomainEvent e, CancellationToken ct)
    {
        log.LogInformation("Event published (in-memory): {EventType}", e.EventType);
        return Task.CompletedTask;
    }
}

