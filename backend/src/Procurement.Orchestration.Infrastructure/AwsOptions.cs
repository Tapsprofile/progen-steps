namespace Procurement.Orchestration.Infrastructure;

public sealed class AwsWorkflowOptions
{
    public string Region { get; init; } = "eu-west-2";

    /// <summary>
    /// Step Functions state machine ARN for the purchase-request workflow.
    /// </summary>
    public string PurchaseRequestStateMachineArn { get; init; } = "";

    public string EventBusName { get; init; } = "default";

    public string PurchaseRequestsTableName { get; init; } = "PurchaseRequests";
    public string WorkflowTokensTableName { get; init; } = "PurchaseRequestWorkflowTokens";

    /// <summary>
    /// Used by the ASL Catch handler (Config.Sns.WorkflowFailedTopicArn).
    /// </summary>
    public string WorkflowFailedTopicArn { get; init; } = "";

    /// <summary>
    /// Optional: provide thresholds via config so workflow input is fully deterministic/testable.
    /// </summary>
    public ThresholdConfig Thresholds { get; init; } = new();

    public LambdaArns Lambdas { get; init; } = new();

    public sealed class ThresholdConfig
    {
        public decimal SpendAuthLimit { get; init; } = 50000;
        public decimal TeamManagerLimit { get; init; } = 50000;
        public decimal FinanceAndLegal { get; init; } = 300000;
        public decimal HighValueLegal { get; init; } = 1200000;
        public decimal CabinetApproval { get; init; } = 2400000;
    }

    public sealed class LambdaArns
    {
        // Routing / assignment
        public string AssignAndNotifyTeamManager { get; init; } = "";
        public string AssignAndNotifyHeadOfService { get; init; } = "";
        public string AssignAndNotifyFinanceOfficer { get; init; } = "";
        public string AssignAndNotifyLegalMonitoringOfficer { get; init; } = "";
        public string AssignAndNotifyLegalMonitoringOfficerHighValue { get; init; } = "";

        // Human task requests (task token waits)
        public string RequestCabinetApprovalDocument { get; init; } = "";
        public string RequestCabinetReportDocument { get; init; } = "";
        public string RequestBidEvaluationFormH { get; init; } = "";
        public string RequestPart1dApproval { get; init; } = "";
        public string RequestPart2aApproval { get; init; } = "";

        // Procurement service tasks
        public string E1ContractSigning { get; init; } = "";
        public string E2ProactisUpdate { get; init; } = "";
        public string E3ConflictOfInterestChecks { get; init; } = "";
        public string E4SupplierOnboarding { get; init; } = "";
        public string E5AwardNoticeAndStandstill { get; init; } = "";
        public string E6ContractRegisterAndCloseOut { get; init; } = "";
    }
}

