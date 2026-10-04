using Omnia.Shared.Contracts;

namespace Omnia.Client.Services;

public interface IRealtimeClient : IAsyncDisposable
{
    ConnectionStatus Status { get; }

    event EventHandler<ConnectionStatus>? StatusChanged;

    event EventHandler<ClipDto>? ClipCreated;

    event EventHandler<Guid>? ClipDeleted;

    event EventHandler? Reconnected;

    Task ConnectAsync(string accessToken, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
