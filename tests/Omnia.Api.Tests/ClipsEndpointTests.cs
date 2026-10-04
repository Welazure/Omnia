using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;

namespace Omnia.Api.Tests;

[Collection(DatabaseCollection.Name)]
public class ClipsEndpointTests(PostgresContainerFixture fixture)
{
    [Fact]
    public async Task GetClips_ReturnsOnlyCallersClips()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "caller@example.com");
        await CreateClipAsync(client, "caller-clip");
        await CreateClipForNewUserAsync(factory, "intruder@example.com", "intruder-clip");

        var response = await client.GetAsync(ApiRoutes.Clips);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var clips = await response.Content.ReadFromJsonAsync<List<ClipDto>>();
        Assert.NotNull(clips);
        var clip = Assert.Single(clips);
        Assert.Equal("caller-clip", clip.Content);
    }

    [Fact]
    public async Task GetClips_ReturnsNewestFirst()
    {
        var (factory, client, connectionString) = await StartAsync();
        using var _factory = factory;
        var caller = await ApiTestAuth.RegisterAndAuthorizeAsync(client, "order@example.com");
        var userId = caller.User.Id;
        await ClipSeeder.AddClipAsync(connectionString, userId, "oldest", DateTimeOffset.UtcNow.AddMinutes(-10));
        await ClipSeeder.AddClipAsync(connectionString, userId, "newest", DateTimeOffset.UtcNow);

        var response = await client.GetAsync(ApiRoutes.Clips);

        var clips = await response.Content.ReadFromJsonAsync<List<ClipDto>>();
        Assert.NotNull(clips);
        Assert.Equal(new[] { "newest", "oldest" }, clips.Select(clip => clip.Content));
    }

    [Fact]
    public async Task GetClips_WithoutToken_Returns401ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await client.GetAsync(ApiRoutes.Clips);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateClip_Returns201WithClipDto()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "create@example.com");

        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("hello", "device-1"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var clip = await response.Content.ReadFromJsonAsync<ClipDto>();
        Assert.NotNull(clip);
        Assert.NotEqual(Guid.Empty, clip.Id);
        Assert.Equal("hello", clip.Content);
        Assert.Equal("device-1", clip.DeviceId);
        Assert.NotEqual(default, clip.CreatedAt);
    }

    [Fact]
    public async Task CreateClip_PersistsForCaller()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "persist@example.com");

        await CreateClipAsync(client, "persisted");

        var clips = await client.GetFromJsonAsync<List<ClipDto>>(ApiRoutes.Clips);
        Assert.NotNull(clips);
        Assert.Contains(clips, clip => clip.Content == "persisted");
    }

    [Fact]
    public async Task CreateClip_EmptyContent_Returns400ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "empty@example.com");

        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("   ", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateClip_WithoutToken_Returns401ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest("hello", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteClip_OwnClip_Returns204AndRemoves()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "delete@example.com");
        var clip = await CreateClipAsync(client, "remove-me");

        var response = await client.DeleteAsync(ApiRoutes.Clip(clip.Id));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var remaining = await client.GetFromJsonAsync<List<ClipDto>>(ApiRoutes.Clips);
        Assert.NotNull(remaining);
        Assert.DoesNotContain(remaining, candidate => candidate.Id == clip.Id);
    }

    [Fact]
    public async Task DeleteClip_OtherUsersClip_Returns404()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "owner@example.com");
        var clip = await CreateClipAsync(client, "private");

        using var intruder = factory.CreateClient();
        await ApiTestAuth.RegisterAndAuthorizeAsync(intruder, "intruder@example.com");
        var response = await intruder.DeleteAsync(ApiRoutes.Clip(clip.Id));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteClip_OtherUsersClip_Returns404ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;
        await ApiTestAuth.RegisterAndAuthorizeAsync(client, "owner@example.com");
        var clip = await CreateClipAsync(client, "private");

        using var intruder = factory.CreateClient();
        await ApiTestAuth.RegisterAndAuthorizeAsync(intruder, "intruder2@example.com");
        var response = await intruder.DeleteAsync(ApiRoutes.Clip(clip.Id));

        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DeleteClip_WithoutToken_Returns401ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await client.DeleteAsync(ApiRoutes.Clip(Guid.NewGuid()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownRoute_Returns404ProblemDetails()
    {
        var (factory, client, _) = await StartAsync();
        using var _factory = factory;

        var response = await client.GetAsync("does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private async Task<(WebApplicationFactory<Program> Factory, HttpClient Client, string ConnectionString)> StartAsync()
    {
        var connectionString = await fixture.CreateDatabaseAsync();
        var factory = ApiFactory.Create(connectionString);
        return (factory, factory.CreateClient(), connectionString);
    }

    private static async Task<ClipDto> CreateClipAsync(HttpClient client, string content)
    {
        var response = await client.PostAsJsonAsync(ApiRoutes.Clips, new CreateClipRequest(content, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClipDto>())!;
    }

    private static async Task CreateClipForNewUserAsync(
        WebApplicationFactory<Program> factory,
        string email,
        string content)
    {
        using var other = factory.CreateClient();
        await ApiTestAuth.RegisterAndAuthorizeAsync(other, email);
        await CreateClipAsync(other, content);
    }
}
