using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Microsoft.Extensions.Options;
using Procurement.Orchestration.Application;
using Procurement.Orchestration.Domain;

namespace Procurement.Orchestration.Infrastructure;

public sealed class DynamoPurchaseRequestRepository(IAmazonDynamoDB dynamo, IOptions<AwsWorkflowOptions> options)
    : IPurchaseRequestRepository
{
    public async Task Upsert(PurchaseRequest request, CancellationToken ct)
    {
        var item = new Dictionary<string, AttributeValue>
        {
            ["PurchaseRequestId"] = new AttributeValue(request.PurchaseRequestId),
            ["SpendAmount"] = new AttributeValue { N = request.SpendAmount.ToString("0.##") },
            ["ContractValue"] = new AttributeValue { N = request.ContractValue.ToString("0.##") },
            ["CurrentAssignee"] = new AttributeValue(request.CurrentAssignee),
            ["ApprovalStatus"] = new AttributeValue(request.Status.ToString())
        };

        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = options.Value.PurchaseRequestsTableName,
            Item = item
        }, ct);
    }

    public async Task<PurchaseRequest?> Get(string purchaseRequestId, CancellationToken ct)
    {
        var resp = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = options.Value.PurchaseRequestsTableName,
            Key = new Dictionary<string, AttributeValue> { ["PurchaseRequestId"] = new AttributeValue(purchaseRequestId) }
        }, ct);

        if (resp.Item is null || resp.Item.Count == 0) return null;

        var spend = decimal.Parse(resp.Item["SpendAmount"].N);
        var contract = decimal.Parse(resp.Item["ContractValue"].N);
        var currentAssignee = resp.Item.TryGetValue("CurrentAssignee", out var ca) ? ca.S : "UNASSIGNED";
        var statusStr = resp.Item.TryGetValue("ApprovalStatus", out var st) ? st.S : nameof(PurchaseRequestStatus.Draft);
        _ = Enum.TryParse<PurchaseRequestStatus>(statusStr, ignoreCase: true, out var status);

        // Re-hydration uses "CreateAndSubmit" semantics; for demo we keep it simple.
        var pr = PurchaseRequest.CreateAndSubmit(purchaseRequestId, spend, contract, createdBy: currentAssignee);
        if (status != PurchaseRequestStatus.Submitted)
        {
            // Best-effort: we expose status/assignee for queries even if events differ.
            pr.AssignTo(currentAssignee);
        }

        return pr;
    }
}

public sealed class EventBridgePublisher(IAmazonEventBridge eventBridge, IOptions<AwsWorkflowOptions> options) : IEventPublisher
{
    public async Task Publish(DomainEvent e, CancellationToken ct)
    {
        var detail = JsonSerializer.Serialize(e, e.GetType(), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        var req = new PutEventsRequest
        {
            Entries = new List<PutEventsRequestEntry>
            {
                new()
                {
                    EventBusName = options.Value.EventBusName,
                    Source = "procurement.orchestration",
                    DetailType = e.EventType,
                    Detail = detail
                }
            }
        };

        await eventBridge.PutEventsAsync(req, ct);
    }
}

public sealed class DynamoTaskTokenStore(IAmazonDynamoDB dynamo, IOptions<AwsWorkflowOptions> options) : ITaskTokenStore
{
    public async Task SaveToken(string purchaseRequestId, TaskTokenType tokenType, string taskToken, CancellationToken ct)
    {
        await dynamo.PutItemAsync(new PutItemRequest
        {
            TableName = options.Value.WorkflowTokensTableName,
            Item = new Dictionary<string, AttributeValue>
            {
                ["PurchaseRequestId"] = new AttributeValue(purchaseRequestId),
                ["TokenType"] = new AttributeValue(tokenType.ToString()),
                ["TaskToken"] = new AttributeValue(taskToken),
                ["CreatedAtUtc"] = new AttributeValue(DateTimeOffset.UtcNow.ToString("O"))
            }
        }, ct);
    }

    public async Task<string?> GetToken(string purchaseRequestId, TaskTokenType tokenType, CancellationToken ct)
    {
        var resp = await dynamo.GetItemAsync(new GetItemRequest
        {
            TableName = options.Value.WorkflowTokensTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PurchaseRequestId"] = new AttributeValue(purchaseRequestId),
                ["TokenType"] = new AttributeValue(tokenType.ToString())
            }
        }, ct);

        return resp.Item is not null && resp.Item.TryGetValue("TaskToken", out var token) ? token.S : null;
    }

    public Task DeleteToken(string purchaseRequestId, TaskTokenType tokenType, CancellationToken ct)
        => dynamo.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = options.Value.WorkflowTokensTableName,
            Key = new Dictionary<string, AttributeValue>
            {
                ["PurchaseRequestId"] = new AttributeValue(purchaseRequestId),
                ["TokenType"] = new AttributeValue(tokenType.ToString())
            }
        }, ct);
}

public sealed class StepFunctionsCallback(IAmazonStepFunctions sfn) : ITaskTokenCallback
{
    public Task SendSuccess(string taskToken, object output, CancellationToken ct)
        => sfn.SendTaskSuccessAsync(new SendTaskSuccessRequest
        {
            TaskToken = taskToken,
            Output = JsonSerializer.Serialize(output, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        }, ct);
}

public sealed class StepFunctionsWorkflowStarter(IAmazonStepFunctions sfn, IOptions<AwsWorkflowOptions> options) : IWorkflowStarter
{
    public async Task<string> StartPurchaseRequestWorkflow(
        string purchaseRequestId,
        decimal spendAmount,
        decimal contractValue,
        string currentAssignee,
        string approvalStatus,
        string[] part1dAuthorizers,
        string[] part2aAuthorizers,
        CancellationToken ct)
    {
        var opt = options.Value;
        if (string.IsNullOrWhiteSpace(opt.PurchaseRequestStateMachineArn))
            throw new InvalidOperationException("AwsWorkflowOptions.PurchaseRequestStateMachineArn must be configured.");

        var input = new
        {
            PurchaseRequestId = purchaseRequestId,
            SpendAmount = spendAmount,
            ContractValue = contractValue,
            CurrentAssignee = currentAssignee,
            ApprovalStatus = approvalStatus,
            Authorization = new
            {
                Part1dAuthorizers = part1dAuthorizers,
                Part2aAuthorizers = part2aAuthorizers
            },
            Thresholds = opt.Thresholds,
            Config = new
            {
                Authorization = new { MaxConcurrency = 10 },
                Sns = new { WorkflowFailedTopicArn = opt.WorkflowFailedTopicArn },
                Lambdas = opt.Lambdas
            }
        };

        var resp = await sfn.StartExecutionAsync(new StartExecutionRequest
        {
            StateMachineArn = opt.PurchaseRequestStateMachineArn,
            Name = $"{purchaseRequestId}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}",
            Input = JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web))
        }, ct);

        return resp.ExecutionArn;
    }
}

