using Microsoft.AspNetCore.SignalR.Client;
using Omnia.Shared.Realtime;

namespace Omnia.Client.Services;

public sealed class HubConnectionFactory(ApiOptions options) : IHubConnectionFactory
{
    public IHubConnectionAdapter Create(string accessToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken);

        var hubUri = new Uri(options.BaseUri, HubRoutes.Clips.TrimStart('/'));

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri, transport => transport.AccessTokenProvider = () => Task.FromResult<string?>(accessToken))
            .WithAutomaticReconnect()
            .Build();

        return new HubConnectionAdapter(connection);
    }
}
