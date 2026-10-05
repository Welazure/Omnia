namespace Omnia.Client.Services;

public interface IDeviceIdStore
{
    Task<string> GetDeviceIdAsync(CancellationToken cancellationToken = default);
}
