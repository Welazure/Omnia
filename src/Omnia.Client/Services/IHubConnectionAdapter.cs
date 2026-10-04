namespace Omnia.Client.Services;

public interface IHubConnectionAdapter : IAsyncDisposable
{
    event Func<Exception?, Task>? Closed;

    event Func<Exception?, Task>? Reconnecting;

    event Func<string?, Task>? Reconnected;

    Task StartAsync(CancellationToken cancellationToken = default);

    IDisposable On<T>(string methodName, Func<T, Task> handler);
}
