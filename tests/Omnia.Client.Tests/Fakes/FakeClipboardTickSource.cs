using System.Threading.Channels;
using Omnia.Client.Services;

namespace Omnia.Client.Tests.Fakes;

public sealed class FakeClipboardTickSource : IClipboardTickSource
{
    private readonly Channel<bool> channel = Channel.CreateUnbounded<bool>();
    private readonly object gate = new();
    private readonly List<(int Target, TaskCompletionSource Source)> waiters = [];
    private int entries;

    public async Task<bool> WaitForTickAsync(CancellationToken cancellationToken)
    {
        lock (gate)
        {
            entries++;
            CompleteWaiters();
        }

        await channel.Reader.ReadAsync(cancellationToken);
        return true;
    }

    public void Tick() => channel.Writer.TryWrite(true);

    public Task WaitForEntriesAsync(int target)
    {
        lock (gate)
        {
            if (entries >= target)
            {
                return Task.CompletedTask;
            }

            var source = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            waiters.Add((target, source));
            return source.Task;
        }
    }

    private void CompleteWaiters()
    {
        for (var i = waiters.Count - 1; i >= 0; i--)
        {
            if (waiters[i].Target > entries)
            {
                continue;
            }

            waiters[i].Source.TrySetResult();
            waiters.RemoveAt(i);
        }
    }
}
