using Omnia.Shared.Contracts;
using Omnia.Shared.Realtime;

namespace Omnia.Client.Services;

public sealed class RealtimeClient(IHubConnectionFactory connectionFactory) : IRealtimeClient
{
    private readonly List<IDisposable> hubSubscriptions = [];

    private IHubConnectionAdapter? connection;

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public event EventHandler<ClipDto>? ClipCreated;

    public event EventHandler<Guid>? ClipDeleted;

    public event EventHandler? Reconnected;

    public async Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        await DisposeConnectionAsync();

        connection = connectionFactory.Create(accessToken);
        connection.Closed += HandleClosedAsync;
        connection.Reconnecting += HandleReconnectingAsync;
        connection.Reconnected += HandleReconnectedAsync;

        hubSubscriptions.Add(connection.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip =>
        {
            ClipCreated?.Invoke(this, clip);
            return Task.CompletedTask;
        }));
        hubSubscriptions.Add(connection.On<Guid>(nameof(IClipClient.OnClipDeleted), id =>
        {
            ClipDeleted?.Invoke(this, id);
            return Task.CompletedTask;
        }));

        SetStatus(ConnectionStatus.Connecting);
        await connection.StartAsync(cancellationToken);
        SetStatus(ConnectionStatus.Connected);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        await DisposeConnectionAsync();
        SetStatus(ConnectionStatus.Disconnected);
    }

    public async ValueTask DisposeAsync() => await DisposeConnectionAsync();

    private Task HandleClosedAsync(Exception? exception)
    {
        SetStatus(ConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    private Task HandleReconnectingAsync(Exception? exception)
    {
        SetStatus(ConnectionStatus.Reconnecting);
        return Task.CompletedTask;
    }

    private Task HandleReconnectedAsync(string? connectionId)
    {
        SetStatus(ConnectionStatus.Connected);
        Reconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    private void SetStatus(ConnectionStatus status)
    {
        if (Status == status)
        {
            return;
        }

        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    private async Task DisposeConnectionAsync()
    {
        if (connection is null)
        {
            return;
        }

        foreach (var subscription in hubSubscriptions)
        {
            subscription.Dispose();
        }

        hubSubscriptions.Clear();

        var previous = connection;
        connection = null;
        await previous.DisposeAsync();
    }
}
