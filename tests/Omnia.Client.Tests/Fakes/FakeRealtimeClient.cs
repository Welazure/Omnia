using Omnia.Client.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeRealtimeClient : IRealtimeClient
{
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public event EventHandler<ClipDto>? ClipCreated;

    public event EventHandler<Guid>? ClipDeleted;

    public event EventHandler? Reconnected;

    public int ConnectCalls { get; private set; }

    public int DisconnectCalls { get; private set; }

    public string? LastAccessToken { get; private set; }

    public Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        ConnectCalls++;
        LastAccessToken = accessToken;
        SetStatus(ConnectionStatus.Connected);
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        DisconnectCalls++;
        SetStatus(ConnectionStatus.Disconnected);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public void RaiseClipCreated(ClipDto clip) => ClipCreated?.Invoke(this, clip);

    public void RaiseClipDeleted(Guid id) => ClipDeleted?.Invoke(this, id);

    public void RaiseReconnected() => Reconnected?.Invoke(this, EventArgs.Empty);

    public void RaiseStatus(ConnectionStatus newStatus) => SetStatus(newStatus);

    private void SetStatus(ConnectionStatus newStatus)
    {
        Status = newStatus;
        StatusChanged?.Invoke(this, newStatus);
    }
}
