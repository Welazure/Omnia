using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeHubConnectionAdapter : IHubConnectionAdapter
{
    private readonly Dictionary<string, Func<object, Task>> handlers = [];

    public event Func<Exception?, Task>? Closed;

    public event Func<Exception?, Task>? Reconnecting;

    public event Func<string?, Task>? Reconnected;

    public bool Started { get; private set; }

    public bool Disposed { get; private set; }

    public IReadOnlyCollection<string> HandlerNames => handlers.Keys;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        Started = true;
        return Task.CompletedTask;
    }

    public IDisposable On<T>(string methodName, Func<T, Task> handler)
    {
        handlers[methodName] = argument => handler((T)argument);
        return new Subscription(() => handlers.Remove(methodName));
    }

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }

    public Task RaiseClosedAsync(Exception? exception) => Closed?.Invoke(exception) ?? Task.CompletedTask;

    public Task RaiseReconnectingAsync(Exception? exception) => Reconnecting?.Invoke(exception) ?? Task.CompletedTask;

    public Task RaiseReconnectedAsync(string? connectionId) => Reconnected?.Invoke(connectionId) ?? Task.CompletedTask;

    public Task RaiseHubAsync<T>(string methodName, T argument) => handlers[methodName](argument!);

    private sealed class Subscription(Action dispose) : IDisposable
    {
        public void Dispose() => dispose();
    }
}
