using Omnia.Shared.Contracts;

namespace Omnia.Client.Services;

public interface IClipSyncService : IAsyncDisposable
{
    IReadOnlyList<ClipDto> Clips { get; }

    ConnectionStatus Status { get; }

    event EventHandler? ClipsChanged;

    event EventHandler<ConnectionStatus>? StatusChanged;

    Task StartAsync(string accessToken, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task RefreshAsync(CancellationToken cancellationToken = default);
}
