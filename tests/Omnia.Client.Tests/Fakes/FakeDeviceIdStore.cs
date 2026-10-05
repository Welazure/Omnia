using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeDeviceIdStore : IDeviceIdStore
{
    public string DeviceId { get; set; } = "test-device";

    public int GetCalls { get; private set; }

    public Task<string> GetDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        GetCalls++;
        return Task.FromResult(DeviceId);
    }
}
