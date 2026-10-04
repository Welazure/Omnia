namespace Omnia.Client.Services;

public sealed class PeriodicClipboardTickSource : IClipboardTickSource, IDisposable
{
    public static readonly TimeSpan DefaultInterval = TimeSpan.FromMilliseconds(500);

    private readonly PeriodicTimer timer;

    public PeriodicClipboardTickSource(TimeSpan interval) => timer = new PeriodicTimer(interval);

    public async Task<bool> WaitForTickAsync(CancellationToken cancellationToken) =>
        await timer.WaitForNextTickAsync(cancellationToken);

    public void Dispose() => timer.Dispose();
}
