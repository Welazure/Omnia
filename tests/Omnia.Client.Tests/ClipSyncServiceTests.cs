using Microsoft.Extensions.Logging.Abstractions;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class ClipSyncServiceTests
{
    [Fact]
    public async Task StartAsync_ConnectsAndLoadsClips()
    {
        var api = new FakeApiClient { ClipsResult = [Clip("one")] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);

        await service.StartAsync("access-token", CancellationToken.None);

        Assert.Equal(1, realtime.ConnectCalls);
        Assert.Equal("access-token", realtime.LastAccessToken);
        Assert.Equal(1, api.GetClipsCalls);
        Assert.Single(service.Clips);
        Assert.Equal(ConnectionStatus.Connected, service.Status);
    }

    [Fact]
    public async Task StopAsync_DisconnectsRealtime()
    {
        var realtime = new FakeRealtimeClient();
        var service = Create(new FakeApiClient(), realtime);

        await service.StopAsync(CancellationToken.None);

        Assert.Equal(1, realtime.DisconnectCalls);
    }

    [Fact]
    public async Task Reconnected_RefetchesClipsAndReplacesList()
    {
        var api = new FakeApiClient { ClipsResult = [Clip("stale")] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        api.ClipsResult = [Clip("fresh-a"), Clip("fresh-b")];
        realtime.RaiseReconnected();

        Assert.Equal(2, api.GetClipsCalls);
        Assert.Equal(2, service.Clips.Count);
        Assert.Equal("fresh-a", service.Clips[0].Content);
        Assert.Equal("fresh-b", service.Clips[1].Content);
    }

    [Fact]
    public async Task ClipCreated_InsertsNewestFirst()
    {
        var api = new FakeApiClient { ClipsResult = [Clip("old")] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        realtime.RaiseClipCreated(Clip("new"));

        Assert.Equal(2, service.Clips.Count);
        Assert.Equal("new", service.Clips[0].Content);
        Assert.Equal("old", service.Clips[1].Content);
    }

    [Fact]
    public async Task ClipDeleted_RemovesMatchingClip()
    {
        var first = Clip("first");
        var second = Clip("second");
        var api = new FakeApiClient { ClipsResult = [first, second] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        realtime.RaiseClipDeleted(first.Id);

        Assert.Single(service.Clips);
        Assert.Equal(second.Id, service.Clips[0].Id);
    }

    [Fact]
    public async Task Status_MirrorsRealtimeClient()
    {
        var realtime = new FakeRealtimeClient();
        var service = Create(new FakeApiClient(), realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        realtime.RaiseStatus(ConnectionStatus.Reconnecting);

        Assert.Equal(ConnectionStatus.Reconnecting, service.Status);
    }

    [Fact]
    public async Task ClipsChanged_RaisedOnReconnect()
    {
        var realtime = new FakeRealtimeClient();
        var service = Create(new FakeApiClient(), realtime);
        await service.StartAsync("access-token", CancellationToken.None);
        var changes = 0;
        service.ClipsChanged += (_, _) => changes++;

        realtime.RaiseReconnected();

        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Clips_ReturnsSnapshot_UnaffectedByLaterMutations()
    {
        var api = new FakeApiClient { ClipsResult = [Clip("one")] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        var snapshot = service.Clips;
        realtime.RaiseClipCreated(Clip("two"));

        Assert.Single(snapshot);
        Assert.Equal(2, service.Clips.Count);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentWithRealtimeMutations_NeverObservesTornList()
    {
        var api = new FakeApiClient { ClipsResult = [Clip("seed")] };
        var realtime = new FakeRealtimeClient();
        var service = Create(api, realtime);
        await service.StartAsync("access-token", CancellationToken.None);

        api.ClipsResult = Enumerable.Range(0, 500).Select(index => Clip($"refreshed-{index}")).ToList();

        var barrier = new Barrier(3);
        var refresher = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < 500; i++)
            {
                service.RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        });
        var creator = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < 500; i++)
            {
                realtime.RaiseClipCreated(Clip($"realtime-{i}"));
            }
        });
        var reader = Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < 500; i++)
            {
                Assert.All(service.Clips, clip => Assert.False(string.IsNullOrEmpty(clip.Content)));
            }
        });

        await Task.WhenAll(refresher, creator, reader);

        Assert.NotEmpty(service.Clips);
    }

    private static ClipSyncService Create(FakeApiClient api, FakeRealtimeClient realtime) =>
        new(api, realtime, NullLogger<ClipSyncService>.Instance);

    private static ClipDto Clip(string content) => new(Guid.NewGuid(), content, null, DateTimeOffset.UtcNow);
}
