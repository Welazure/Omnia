using Omnia.Client.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeClipSyncService : IClipSyncService
{
    private ConnectionStatus status = ConnectionStatus.Disconnected;

    public IReadOnlyList<ClipDto> Clips { get; set; } = [];

    public ConnectionStatus Status
    {
        get => status;
        set
        {
            status = value;
            StatusChanged?.Invoke(this, value);
        }
    }

    public event EventHandler? ClipsChanged;

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public int StartCalls { get; private set; }

    public string? LastAccessToken { get; private set; }

    public Exception? ThrowOnStart { get; set; }

    public Task StartAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        StartCalls++;
        LastAccessToken = accessToken;

        if (ThrowOnStart is not null)
        {
            return Task.FromException(ThrowOnStart);
        }

        Status = ConnectionStatus.Connected;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        ClipsChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
