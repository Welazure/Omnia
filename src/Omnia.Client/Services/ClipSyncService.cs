using Microsoft.Extensions.Logging;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Services;

public sealed class ClipSyncService : IClipSyncService
{
    private readonly IApiClient apiClient;
    private readonly IRealtimeClient realtimeClient;
    private readonly ILogger<ClipSyncService> logger;

    // Serializes list mutation: refreshes run on the caller's thread while clip events fire on SignalR threads.
    // Readers take an immutable snapshot under the same gate, so a subscriber never observes a torn list.
    private readonly object gate = new();
    private readonly List<ClipDto> clips = [];

    public ClipSyncService(IApiClient apiClient, IRealtimeClient realtimeClient, ILogger<ClipSyncService> logger)
    {
        this.apiClient = apiClient;
        this.realtimeClient = realtimeClient;
        this.logger = logger;

        realtimeClient.ClipCreated += HandleClipCreated;
        realtimeClient.ClipDeleted += HandleClipDeleted;
        realtimeClient.Reconnected += HandleReconnected;
        realtimeClient.StatusChanged += HandleStatusChanged;
    }

    public IReadOnlyList<ClipDto> Clips
    {
        get
        {
            lock (gate)
            {
                return clips.ToArray();
            }
        }
    }

    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;

    public event EventHandler? ClipsChanged;

    public event EventHandler<ConnectionStatus>? StatusChanged;

    public async Task StartAsync(string accessToken, CancellationToken cancellationToken = default)
    {
        await realtimeClient.ConnectAsync(accessToken, cancellationToken);
        await RefreshAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken = default) => realtimeClient.DisconnectAsync(cancellationToken);

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var refreshed = await apiClient.GetClipsAsync(cancellationToken);

        lock (gate)
        {
            clips.Clear();
            clips.AddRange(refreshed);
        }

        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async ValueTask DisposeAsync()
    {
        realtimeClient.ClipCreated -= HandleClipCreated;
        realtimeClient.ClipDeleted -= HandleClipDeleted;
        realtimeClient.Reconnected -= HandleReconnected;
        realtimeClient.StatusChanged -= HandleStatusChanged;

        await realtimeClient.DisposeAsync();
    }

    private void HandleClipCreated(object? sender, ClipDto clip)
    {
        lock (gate)
        {
            clips.Insert(0, clip);
        }

        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleClipDeleted(object? sender, Guid id)
    {
        lock (gate)
        {
            clips.RemoveAll(clip => clip.Id == id);
        }

        ClipsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void HandleReconnected(object? sender, EventArgs e) => _ = ReconcileAsync();

    private async Task ReconcileAsync()
    {
        try
        {
            await RefreshAsync();
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Reconciling clips after reconnect failed.");
        }
    }

    private void HandleStatusChanged(object? sender, ConnectionStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }
}
