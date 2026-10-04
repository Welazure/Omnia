using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Shared.Contracts;
using Omnia.Shared.Realtime;

namespace Omnia.Client.Tests;

public sealed class RealtimeClientTests
{
    [Fact]
    public async Task ConnectAsync_PassesTokenToFactoryAndStartsConnection()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);

        await client.ConnectAsync("access-token", CancellationToken.None);

        Assert.Equal(1, factory.CreateCalls);
        Assert.Equal("access-token", factory.LastAccessToken);
        Assert.True(factory.Adapter.Started);
    }

    [Fact]
    public async Task ConnectAsync_StartSucceeds_ReportsConnected()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);

        await client.ConnectAsync("access-token", CancellationToken.None);

        Assert.Equal(ConnectionStatus.Connected, client.Status);
    }

    [Fact]
    public async Task ConnectAsync_RegistersClipHandlers()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);

        await client.ConnectAsync("access-token", CancellationToken.None);

        Assert.Contains(nameof(IClipClient.OnClipCreated), factory.Adapter.HandlerNames);
        Assert.Contains(nameof(IClipClient.OnClipDeleted), factory.Adapter.HandlerNames);
    }

    [Fact]
    public async Task ConnectAsync_BlankToken_Throws()
    {
        var client = new RealtimeClient(new FakeHubConnectionFactory());

        await Assert.ThrowsAsync<ArgumentException>(() => client.ConnectAsync(" ", CancellationToken.None));
    }

    [Fact]
    public async Task Reconnecting_ReportsReconnecting()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);
        await client.ConnectAsync("access-token", CancellationToken.None);

        await factory.Adapter.RaiseReconnectingAsync(new Exception("dropped"));

        Assert.Equal(ConnectionStatus.Reconnecting, client.Status);
    }

    [Fact]
    public async Task Reconnected_ReportsConnectedAndRaisesReconnectedEvent()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);
        await client.ConnectAsync("access-token", CancellationToken.None);
        await factory.Adapter.RaiseReconnectingAsync(new Exception("dropped"));
        var reconnectedRaised = false;
        client.Reconnected += (_, _) => reconnectedRaised = true;

        await factory.Adapter.RaiseReconnectedAsync("connection-id");

        Assert.Equal(ConnectionStatus.Connected, client.Status);
        Assert.True(reconnectedRaised);
    }

    [Fact]
    public async Task Closed_ReportsDisconnected()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);
        await client.ConnectAsync("access-token", CancellationToken.None);

        await factory.Adapter.RaiseClosedAsync(new Exception("closed"));

        Assert.Equal(ConnectionStatus.Disconnected, client.Status);
    }

    [Fact]
    public async Task ClipCreatedFromHub_RaisesClipCreatedEvent()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);
        await client.ConnectAsync("access-token", CancellationToken.None);
        var clip = new ClipDto(Guid.NewGuid(), "hello", null, DateTimeOffset.UtcNow);
        ClipDto? received = null;
        client.ClipCreated += (_, created) => received = created;

        await factory.Adapter.RaiseHubAsync(nameof(IClipClient.OnClipCreated), clip);

        Assert.Equal(clip.Id, received?.Id);
    }

    [Fact]
    public async Task DisconnectAsync_SetsDisconnectedAndDisposesConnection()
    {
        var factory = new FakeHubConnectionFactory();
        var client = new RealtimeClient(factory);
        await client.ConnectAsync("access-token", CancellationToken.None);

        await client.DisconnectAsync(CancellationToken.None);

        Assert.Equal(ConnectionStatus.Disconnected, client.Status);
        Assert.True(factory.Adapter.Disposed);
    }
}
