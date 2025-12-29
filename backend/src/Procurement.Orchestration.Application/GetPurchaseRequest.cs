using Procurement.Orchestration.Domain;

namespace Procurement.Orchestration.Application;

public sealed record GetPurchaseRequestQuery(string PurchaseRequestId) : IQuery<PurchaseRequest?>;

public sealed class GetPurchaseRequestHandler(IPurchaseRequestRepository repository)
    : IQueryHandler<GetPurchaseRequestQuery, PurchaseRequest?>
{
    public Task<PurchaseRequest?> Handle(GetPurchaseRequestQuery query, CancellationToken ct)
        => repository.Get(query.PurchaseRequestId, ct);
}

