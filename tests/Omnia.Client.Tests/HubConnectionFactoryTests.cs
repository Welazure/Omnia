using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Client;
using Omnia.Client.Services;

namespace Omnia.Client.Tests;

public sealed class HubConnectionFactoryTests
{
    [Fact]
    public void ConfigureOptions_WhenForceWebSockets_SelectsWebSocketsAndSkipsNegotiation()
    {
        var factory = new HubConnectionFactory(new ApiOptions { ForceWebSockets = true });
        var connectionOptions = new HttpConnectionOptions();

        factory.ConfigureOptions(connectionOptions, "access-token");

        Assert.Equal(HttpTransportType.WebSockets, connectionOptions.Transports);
        Assert.True(connectionOptions.SkipNegotiation);
    }

    [Fact]
    public void ConfigureOptions_WhenNotForced_LeavesTransportUnset()
    {
        var factory = new HubConnectionFactory(new ApiOptions());
        var connectionOptions = new HttpConnectionOptions();
        var defaultTransports = connectionOptions.Transports;

        factory.ConfigureOptions(connectionOptions, "access-token");

        Assert.Equal(defaultTransports, connectionOptions.Transports);
        Assert.NotEqual(HttpTransportType.WebSockets, connectionOptions.Transports);
        Assert.False(connectionOptions.SkipNegotiation);
    }

    [Fact]
    public async Task ConfigureOptions_AlwaysSetsAccessTokenProvider()
    {
        var factory = new HubConnectionFactory(new ApiOptions());
        var connectionOptions = new HttpConnectionOptions();

        factory.ConfigureOptions(connectionOptions, "access-token");

        var provider = connectionOptions.AccessTokenProvider;
        Assert.NotNull(provider);
        Assert.Equal("access-token", await provider!.Invoke());
    }

    [Fact]
    public void Create_ReturnsAdapter()
    {
        var options = new ApiOptions { BaseUrl = "http://localhost:8080", ForceWebSockets = true };
        var factory = new HubConnectionFactory(options);

        var adapter = factory.Create("access-token");

        Assert.NotNull(adapter);
    }
}
