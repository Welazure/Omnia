using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;
using Omnia.Shared.Realtime;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class ClipSyncLatencyTests(PostgresContainerFixture fixture)
{
    private static readonly TimeSpan SyncBudget = TimeSpan.FromSeconds(2);

    [Fact]
    public async Task SameAccount_TwoHubClients_ReceiveClipWithinTwoSeconds()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        using var factory = ApiFactory.Create(connectionString);
        using var client = factory.CreateClient();
        var auth = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "latency@example.com");

        var received = new TaskCompletionSource<ClipDto>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var sender = BuildConnection(factory, auth.Token);
        await using var receiver = BuildConnection(factory, auth.Token);
        receiver.On<ClipDto>(nameof(IClipClient.OnClipCreated), clip => received.TrySetResult(clip));

        await sender.StartAsync();
        await receiver.StartAsync();
        await Task.Delay(200);

        var stopwatch = Stopwatch.StartNew();
        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("sync-now", "web"));
        response.EnsureSuccessStatusCode();

        var clip = await received.Task.WaitAsync(SyncBudget);
        stopwatch.Stop();

        Assert.Equal("sync-now", clip.Content);
        Assert.True(stopwatch.Elapsed < SyncBudget, $"Sync took {stopwatch.ElapsedMilliseconds} ms, exceeding the 2s budget.");
    }

    private static HubConnection BuildConnection(WebApplicationFactory<Program> factory, string accessToken)
    {
        var url = $"{factory.Server.BaseAddress}{HubRoutes.Clips.TrimStart('/')}?access_token={Uri.EscapeDataString(accessToken)}";

        return new HubConnectionBuilder()
            .WithUrl(url, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }
}
