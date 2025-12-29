using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Procurement.Orchestration.Application;

namespace Procurement.Orchestration.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddProcurementOrchestrationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Handwritten engine configuration
        services.Configure<HandwrittenWorkflowOptions>(configuration.GetSection("HandwrittenWorkflow"));

        // In-memory adapters (replace with DB/event-store implementations for production)
        services.AddSingleton<IPurchaseRequestRepository, InMemoryPurchaseRequestRepository>();
        services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();

        // Handwritten runtime
        services.AddSingleton<IWorkflowRuntime, HandwrittenWorkflowRuntime>();

        return services;
    }
}

