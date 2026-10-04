using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using System.Threading.Tasks;
using Omnia.Client.Services;

namespace Omnia.Client.Browser.Services;

public sealed partial class BrowserTokenStore : ITokenStore
{
    private const string StorageKey = "omnia.token";

    public Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        var token = GetItem(StorageKey);
        return Task.FromResult(string.IsNullOrWhiteSpace(token) ? null : token);
    }

    public Task SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        SetItem(StorageKey, token);

        return Task.CompletedTask;
    }

    public Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        RemoveItem(StorageKey);

        return Task.CompletedTask;
    }

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetItem(string key);

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetItem(string key, string value);

    [JSImport("globalThis.localStorage.removeItem")]
    private static partial void RemoveItem(string key);
}
