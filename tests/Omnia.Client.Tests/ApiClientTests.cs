using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Omnia.Client.Services;
using Omnia.Client.Tests.Fakes;
using Omnia.Shared.Contracts;

namespace Omnia.Client.Tests;

public sealed class ApiClientTests
{
    private static readonly UserDto User = new(Guid.NewGuid(), "user@example.com");

    [Fact]
    public async Task RegisterAsync_Success_ReturnsAuthResponse()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(new AuthResponse("token-abc", User))
        });

        var result = await client.RegisterAsync("user@example.com", "password", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("token-abc", result.Token);
        Assert.Equal(User.Id, result.User.Id);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ReturnsNull()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Conflict));

        Assert.Null(await client.RegisterAsync("user@example.com", "password", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_Success_ReturnsAuthResponse()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthResponse("token-xyz", User))
        });

        var result = await client.LoginAsync("user@example.com", "password", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("token-xyz", result.Token);
    }

    [Fact]
    public async Task LoginAsync_InvalidCredentials_ReturnsNull()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Assert.Null(await client.LoginAsync("user@example.com", "wrong", CancellationToken.None));
    }

    [Fact]
    public async Task GetMeAsync_Success_ReturnsUser()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(User)
        });

        var result = await client.GetMeAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(User.Email, result.Email);
    }

    [Fact]
    public async Task GetMeAsync_Unauthorized_ReturnsNull()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Assert.Null(await client.GetMeAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetClipsAsync_SendsStoredBearerToken()
    {
        var tokenStore = new FakeTokenStore { Token = "stored-token" };
        HttpRequestMessage? captured = null;
        var client = CreateClient(request =>
        {
            captured = request;
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(Array.Empty<ClipDto>()) };
        }, tokenStore);

        await client.GetClipsAsync(CancellationToken.None);

        Assert.NotNull(captured);
        Assert.Equal("Bearer", captured.Headers.Authorization?.Scheme);
        Assert.Equal("stored-token", captured.Headers.Authorization?.Parameter);
    }

    [Fact]
    public async Task GetClipsAsync_ReturnsDeserializedClips()
    {
        var clip = new ClipDto(Guid.NewGuid(), "hello", "device", DateTimeOffset.UtcNow);
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new[] { clip })
        });

        var result = await client.GetClipsAsync(CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(clip.Id, result[0].Id);
        Assert.Equal("hello", result[0].Content);
    }

    [Fact]
    public async Task CreateClipAsync_Created_ReturnsClip()
    {
        var clip = new ClipDto(Guid.NewGuid(), "hello", null, DateTimeOffset.UtcNow);
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.Created)
        {
            Content = JsonContent.Create(clip)
        });

        var result = await client.CreateClipAsync("hello", CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(clip.Id, result.Id);
    }

    [Fact]
    public async Task CreateClipAsync_AttachesPersistedDeviceId()
    {
        CreateClipRequest? body = null;
        var deviceStore = new FakeDeviceIdStore { DeviceId = "device-xyz" };
        var clip = new ClipDto(Guid.NewGuid(), "hello", "device-xyz", DateTimeOffset.UtcNow);
        var client = CreateClient(
            request =>
            {
                body = request.Content!.ReadFromJsonAsync<CreateClipRequest>().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.Created) { Content = JsonContent.Create(clip) };
            },
            deviceStore: deviceStore);

        var result = await client.CreateClipAsync("hello", CancellationToken.None);

        Assert.NotNull(body);
        Assert.Equal("hello", body.Content);
        Assert.Equal("device-xyz", body.DeviceId);
        Assert.Equal(1, deviceStore.GetCalls);
        Assert.NotNull(result);
        Assert.Equal("device-xyz", result.DeviceId);
    }

    [Fact]
    public async Task DeleteClipAsync_NoContent_ReturnsTrue()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NoContent));

        Assert.True(await client.DeleteClipAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteClipAsync_NotFound_ReturnsFalse()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.NotFound));

        Assert.False(await client.DeleteClipAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RegisterAsync_ServerError_ThrowsApiException()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<ApiException>(() => client.RegisterAsync("user@example.com", "password", CancellationToken.None));
    }

    [Fact]
    public async Task LoginAsync_ServerError_ThrowsApiException()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

        await Assert.ThrowsAsync<ApiException>(() => client.LoginAsync("user@example.com", "password", CancellationToken.None));
    }

    [Fact]
    public async Task GetClipsAsync_TransportFailure_ThrowsApiExceptionWithInner()
    {
        var failure = new HttpRequestException("connection refused");
        var client = CreateClient(_ => throw failure);

        var exception = await Assert.ThrowsAsync<ApiException>(() => client.GetClipsAsync(CancellationToken.None));

        Assert.Same(failure, exception.InnerException);
    }

    [Fact]
    public async Task GetClipsAsync_ServerError_ThrowsApiException()
    {
        var client = CreateClient(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await Assert.ThrowsAsync<ApiException>(() => client.GetClipsAsync(CancellationToken.None));
    }

    private static ApiClient CreateClient(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        FakeTokenStore? tokenStore = null,
        FakeDeviceIdStore? deviceStore = null)
    {
        var handler = new FakeHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        return new ApiClient(httpClient, tokenStore ?? new FakeTokenStore(), deviceStore ?? new FakeDeviceIdStore());
    }
}
