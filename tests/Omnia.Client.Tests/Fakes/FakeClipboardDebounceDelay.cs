namespace Omnia.Client.Tests.Fakes;

// Stands in for the debounce wait. Each call parks until Release() opens the currently pending
// gates, so tests control exactly when a debounce window elapses without a real timer.
public sealed class FakeClipboardDebounceDelay
{
    private readonly object gate = new();
    private readonly List<TaskCompletionSource> pending = [];

    public int DelayCalls { get; private set; }

    public Task WaitAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        lock (gate)
        {
            DelayCalls++;
            var source = new TaskCompletionSource();
            pending.Add(source);
            return source.Task;
        }
    }

    public void Release()
    {
        TaskCompletionSource[] toComplete;
        lock (gate)
        {
            toComplete = pending.ToArray();
            pending.Clear();
        }

        foreach (var source in toComplete)
        {
            source.TrySetResult();
        }
    }
}
