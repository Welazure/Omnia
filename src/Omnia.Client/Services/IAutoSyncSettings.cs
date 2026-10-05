namespace Omnia.Client.Services;

public interface IAutoSyncSettings
{
    Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default);

    Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default);
}
