using Amazon.DynamoDBv2;
using Amazon.EventBridge;
using Amazon.StepFunctions;
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
        services.Configure<AwsWorkflowOptions>(configuration.GetSection("AwsWorkflow"));

        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonEventBridge>();
        services.AddAWSService<IAmazonStepFunctions>();

        services.AddSingleton<IPurchaseRequestRepository, DynamoPurchaseRequestRepository>();
        services.AddSingleton<IEventPublisher, EventBridgePublisher>();
        services.AddSingleton<ITaskTokenStore, DynamoTaskTokenStore>();
        services.AddSingleton<ITaskTokenCallback, StepFunctionsCallback>();
        services.AddSingleton<IWorkflowStarter, StepFunctionsWorkflowStarter>();

        return services;
    }
}

