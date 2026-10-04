using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Omnia.Shared.Contracts;
using Omnia.Shared.Http;

namespace Omnia.Client.Services;

public sealed class ApiClient(HttpClient httpClient, ITokenStore tokenStore) : IApiClient
{
    public Task<AuthResponse?> RegisterAsync(string email, string password, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            using var response = await httpClient.PostAsJsonAsync(
                ApiRoutes.Register,
                new RegisterRequest(email, password),
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                return null;
            }

            EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        }, cancellationToken);

    public Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            using var response = await httpClient.PostAsJsonAsync(
                ApiRoutes.Login,
                new LoginRequest(email, password),
                cancellationToken);

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                return null;
            }

            EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken);
        }, cancellationToken);

    public Task<UserDto?> GetMeAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, ApiRoutes.Me, cancellationToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return null;
            }

            EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<UserDto>(cancellationToken);
        }, cancellationToken);

    public Task<IReadOnlyList<ClipDto>> GetClipsAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync<IReadOnlyList<ClipDto>>(async () =>
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, ApiRoutes.Clips, cancellationToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            EnsureSuccess(response);

            return await response.Content.ReadFromJsonAsync<List<ClipDto>>(cancellationToken) ?? [];
        }, cancellationToken);

    public Task<ClipDto?> CreateClipAsync(string content, string? deviceId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Post, ApiRoutes.Clips, cancellationToken);
            request.Content = JsonContent.Create(new CreateClipRequest(content, deviceId));

            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                return null;
            }

            EnsureSuccess(response);
            return await response.Content.ReadFromJsonAsync<ClipDto>(cancellationToken);
        }, cancellationToken);

    public Task<bool> DeleteClipAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async () =>
        {
            using var request = await CreateAuthorizedRequestAsync(HttpMethod.Delete, ApiRoutes.Clip(id), cancellationToken);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            EnsureSuccess(response);
            return true;
        }, cancellationToken);

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string route,
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(method, route);

        var token = await tokenStore.GetTokenAsync(cancellationToken);
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }

    private static async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken)
    {
        try
        {
            return await operation();
        }
        catch (HttpRequestException exception)
        {
            throw new ApiException("Could not reach the server.", exception);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiException("The server did not respond in time.", exception);
        }
    }

    private static void EnsureSuccess(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new ApiException($"The server responded with status {(int)response.StatusCode}.");
        }
    }
}
