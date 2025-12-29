namespace Procurement.Orchestration.Application;

using Microsoft.Extensions.DependencyInjection;

public interface ICommand<out TResponse> { }
public interface IQuery<out TResponse> { }

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{
    Task<TResponse> Handle(TCommand command, CancellationToken ct);
}

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{
    Task<TResponse> Handle(TQuery query, CancellationToken ct);
}

public interface IDispatcher
{
    Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default);
    Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default);
}

public sealed class Dispatcher(IServiceProvider services) : IDispatcher
{
    public Task<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken ct = default)
    {
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(command.GetType(), typeof(TResponse));
        dynamic handler = services.GetRequiredService(handlerType);
        return handler.Handle((dynamic)command, ct);
    }

    public Task<TResponse> Query<TResponse>(IQuery<TResponse> query, CancellationToken ct = default)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResponse));
        dynamic handler = services.GetRequiredService(handlerType);
        return handler.Handle((dynamic)query, ct);
    }
}

