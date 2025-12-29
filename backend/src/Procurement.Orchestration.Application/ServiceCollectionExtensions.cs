using Microsoft.Extensions.DependencyInjection;

namespace Procurement.Orchestration.Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcurementOrchestrationApplication(this IServiceCollection services)
    {
        services.AddSingleton<IDispatcher, Dispatcher>();

        // CQRS registrations
        services.AddSingleton<ICommandHandler<CreatePurchaseRequestCommand, CreatePurchaseRequestResult>, CreatePurchaseRequestHandler>();
        services.AddSingleton<IQueryHandler<GetPurchaseRequestQuery, Domain.PurchaseRequest?>, GetPurchaseRequestHandler>();
        services.AddSingleton<ICommandHandler<SubmitCallbackCommand, SubmitCallbackResult>, SubmitCallbackHandler>();

        return services;
    }
}

