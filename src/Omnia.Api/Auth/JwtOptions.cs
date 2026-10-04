using System.Text;

namespace Omnia.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    private const int MinimumSecretBytes = 32;

    public string Secret { get; set; } = string.Empty;

    public string Issuer { get; set; } = "omnia";

    public string Audience { get; set; } = "omnia-clients";

    public TimeSpan Lifetime { get; set; } = TimeSpan.FromDays(7);

    public void Validate()
    {
        if (Encoding.UTF8.GetByteCount(Secret) < MinimumSecretBytes)
        {
            throw new InvalidOperationException(
                $"Configuration '{SectionName}:Secret' must be set to at least {MinimumSecretBytes} bytes.");
        }
    }
}
