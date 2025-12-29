namespace Procurement.Orchestration.Application;

public sealed record SubmitCallbackCommand(
    string PurchaseRequestId,
    TaskTokenType TokenType,
    object Output) : ICommand<SubmitCallbackResult>;

public sealed record SubmitCallbackResult(string PurchaseRequestId, TaskTokenType TokenType, bool Completed);

public sealed class SubmitCallbackHandler(ITaskTokenStore store, ITaskTokenCallback callback)
    : ICommandHandler<SubmitCallbackCommand, SubmitCallbackResult>
{
    public async Task<SubmitCallbackResult> Handle(SubmitCallbackCommand command, CancellationToken ct)
    {
        var token = await store.GetToken(command.PurchaseRequestId, command.TokenType, ct);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new SubmitCallbackResult(command.PurchaseRequestId, command.TokenType, Completed: false);
        }

        await callback.SendSuccess(token, command.Output, ct);
        await store.DeleteToken(command.PurchaseRequestId, command.TokenType, ct);
        return new SubmitCallbackResult(command.PurchaseRequestId, command.TokenType, Completed: true);
    }
}

