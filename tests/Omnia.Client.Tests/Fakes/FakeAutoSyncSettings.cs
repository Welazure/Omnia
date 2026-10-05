using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeAutoSyncSettings : IAutoSyncSettings
{
    public bool IsEnabled { get; set; } = true;

    public int GetCalls { get; private set; }

    public int SetCalls { get; private set; }

    public Task<bool> GetEnabledAsync(CancellationToken cancellationToken = default)
    {
        GetCalls++;
        return Task.FromResult(IsEnabled);
    }

    public Task SetEnabledAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        SetCalls++;
        IsEnabled = enabled;
        return Task.CompletedTask;
    }
}
