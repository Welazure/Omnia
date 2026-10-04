using Microsoft.AspNetCore.SignalR.Client;

namespace Omnia.Client.Services;

public sealed class HubConnectionAdapter : IHubConnectionAdapter
{
    private readonly HubConnection connection;

    public HubConnectionAdapter(HubConnection connection)
    {
        this.connection = connection;
        connection.Closed += HandleClosedAsync;
        connection.Reconnecting += HandleReconnectingAsync;
        connection.Reconnected += HandleReconnectedAsync;
    }

    public event Func<Exception?, Task>? Closed;

    public event Func<Exception?, Task>? Reconnecting;

    public event Func<string?, Task>? Reconnected;

    public Task StartAsync(CancellationToken cancellationToken = default) => connection.StartAsync(cancellationToken);

    public IDisposable On<T>(string methodName, Func<T, Task> handler) => connection.On(methodName, handler);

    public ValueTask DisposeAsync() => connection.DisposeAsync();

    private Task HandleClosedAsync(Exception? exception) => Closed?.Invoke(exception) ?? Task.CompletedTask;

    private Task HandleReconnectingAsync(Exception? exception) => Reconnecting?.Invoke(exception) ?? Task.CompletedTask;

    private Task HandleReconnectedAsync(string? connectionId) => Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;
}
