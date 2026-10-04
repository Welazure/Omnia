using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Omnia.Api.Hubs;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;
using Omnia.Shared.Realtime;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class ClipHubTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task ClipHub_WithAccessTokenQueryString_Connects()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        var auth = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "hub@example.com");

        await using var connection = BuildConnection(factory, auth.Token);
        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task ClipHub_WithoutToken_FailsToConnect()
    {
        var (factory, _, _) = await StartAsync();
        using var _factory = factory;

        await using var connection = BuildConnection(factory, null);

        await Assert.ThrowsAnyAsync<Exception>(() => connection.StartAsync());
    }

    [Fact]
    public async Task ClipHub_IsInGroupNamedByUserId()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        var auth = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "group@example.com");

        var clipForUser = new ClipDto(Guid.NewGuid(), "mine", null, DateTimeOffset.UtcNow);
        var clipForOther = new ClipDto(Guid.NewGuid(), "not-mine", null, DateTimeOffset.UtcNow);
        var userGroupEvent = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var otherGroupEvent = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connection = BuildConnection(factory, auth.Token);
        connection.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip =>
        {
            if (clip.Id == clipForUser.Id)
            {
                userGroupEvent.TrySetResult(clip);
            }

            if (clip.Id == clipForOther.Id)
            {
                otherGroupEvent.TrySetResult(clip);
            }
        });
        await StartAndJoinAsync(connection);

        var hub = factory.Services.GetRequiredService<IHubContext<ClipHub, IClipClient>>();
        await hub.Clients.Group(ClipHub.GroupName(Guid.NewGuid())).OnClipCreated(clipForOther);
        await hub.Clients.Group(ClipHub.GroupName(auth.User.Id)).OnClipCreated(clipForUser);

        var received = await userGroupEvent.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(clipForUser.Id, received.Id);

        await Task.Delay(200);
        Assert.False(otherGroupEvent.Task.IsCompleted);
    }

    [Fact]
    public async Task CreateClip_BroadcastsOnClipCreatedToCaller()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        var auth = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "broadcast@example.com");

        var created = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = BuildConnection(factory, auth.Token);
        connection.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip => created.TrySetResult(clip));
        await StartAndJoinAsync(connection);

        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("live", "phone"));
        response.EnsureSuccessStatusCode();

        var clip = await created.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal("live", clip.Content);
        Assert.Equal("phone", clip.DeviceId);
    }

    [Fact]
    public async Task CreateClip_DoesNotBroadcastToOtherUser()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        var author = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "author@example.com");

        using var bystanderClient = factory.CreateClient();
        var bystander = await ApiTestAuth.RegisterAndAuthorizeAsync(bystanderClient, "bystander@example.com");

        var authorReceived = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bystanderReceived = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var authorConnection = BuildConnection(factory, author.Token);
        authorConnection.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip => authorReceived.TrySetResult(clip));
        await StartAndJoinAsync(authorConnection);

        await using var bystanderConnection = BuildConnection(factory, bystander.Token);
        bystanderConnection.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip => bystanderReceived.TrySetResult(clip));
        await StartAndJoinAsync(bystanderConnection);

        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("private", null));
        response.EnsureSuccessStatusCode();

        await authorReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(200);
        Assert.False(bystanderReceived.Task.IsCompleted);
    }

    [Fact]
    public async Task DeleteClip_BroadcastsOnClipDeletedToCaller()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        var auth = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "deleter@example.com");

        var clipResponse = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("bye", null));
        clipResponse.EnsureSuccessStatusCode();
        var clip = (await clipResponse.Content.ReadFromJsonAsync<ClipDto>())!;

        var deleted = new TaskCompletionSource<Guid>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var connection = BuildConnection(factory, auth.Token);
        connection.On<Guid>(nameof(IClipClient.OnClipDeleted), id => deleted.TrySetResult(id));
        await StartAndJoinAsync(connection);

        var response = await client.DeleteAsync(ApiRoutes.Clip(clip.Id));
        response.EnsureSuccessStatusCode();

        var deletedId = await deleted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(clip.Id, deletedId);
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, string ConnectionString)> StartAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var factory = ApiFactory.Create(connectionString);
        return (factory, factory.CreateClient(), connectionString);
    }

    private static HubConnection BuildConnection(WebApplicationFactory<Program> factory, string? accessToken)
    {
        var url = $"{factory.Server.BaseAddress}{HubRoutes.Clips.TrimStart('/')}";
        if (accessToken is not null)
        {
            url += $"?access_token={Uri.EscapeDataString(accessToken)}";
        }

        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    private static async Task StartAndJoinAsync(HubConnection connection)
    {
        await connection.StartAsync();
        await Task.Delay(200);
    }
}
