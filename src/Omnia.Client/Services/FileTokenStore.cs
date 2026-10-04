using System.IO;

namespace Omnia.Client.Services;

public sealed class FileTokenStore : ITokenStore
{
    private const string TokenFileName = "token.dat";

    private readonly string directory;

    public FileTokenStore(string? directory = null)
    {
        this.directory = directory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Omnia");
    }

    private string TokenPath => Path.Combine(directory, TokenFileName);

    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(TokenPath))
        {
            return null;
        }

        var token = await File.ReadAllTextAsync(TokenPath, cancellationToken);
        return string.IsNullOrWhiteSpace(token) ? null : token.Trim();
    }

    public async Task SaveTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        Directory.CreateDirectory(directory);

        var temporaryPath = TokenPath + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, token, cancellationToken);
        File.Move(temporaryPath, TokenPath, overwrite: true);
    }

    public Task ClearTokenAsync(CancellationToken cancellationToken = default)
    {
        if (File.Exists(TokenPath))
        {
            File.Delete(TokenPath);
        }

        return Task.CompletedTask;
    }
}
