using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Omnia.Shared.Http;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class CorsTests(PostgresContainerFixture fixture)
{
    private const string AllowedOrigin = "http://localhost:5000";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_AllowsCredentials()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await PreflightAsync(client, AllowedOrigin);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(AllowedOrigin, response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Equal("true", response.Headers.GetValues("Access-Control-Allow-Credentials").Single());
    }

    [Fact]
    public async Task Preflight_FromUnknownOrigin_IsNotAllowed()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await PreflightAsync(client, "http://evil.example.com");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    private static async Task<HttpResponseMessage> PreflightAsync(HttpClient client, string origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, ApiRoutes.Clips);
        request.Headers.Add("Origin", origin);
        request.Headers.Add("Access-Control-Request-Method", "GET");
        return await client.SendAsync(request);
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, string ConnectionString)> StartAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var factory = ApiFactory.Create(connectionString);
        return (factory, factory.CreateClient(), connectionString);
    }
}
