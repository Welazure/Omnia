using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
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
            .WithUrl(hubUri, connectionOptions => ConfigureOptions(connectionOptions, accessToken))
            .WithAutomaticReconnect()
            .Build();

        return new HubConnectionAdapter(connection);
    }

    internal void ConfigureOptions(HttpConnectionOptions connectionOptions, string accessToken)
    {
        connectionOptions.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);

        if (options.ForceWebSockets)
        {
            connectionOptions.Transports = HttpTransportType.WebSockets;
            connectionOptions.SkipNegotiation = true;
        }
    }
}
