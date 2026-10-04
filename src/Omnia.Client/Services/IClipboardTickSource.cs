namespace Omnia.Client.Services;

public interface IClipboardTickSource
{
    Task<bool> WaitForTickAsync(CancellationToken cancellationToken);
}
