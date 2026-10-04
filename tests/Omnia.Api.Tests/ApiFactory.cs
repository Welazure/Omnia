using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace Omnia.Api.Tests;

internal static class ApiFactory
{
    public const string JwtSecret = "omnia-integration-test-secret-0123456789abcdef";
    public const string JwtIssuer = "omnia-tests";
    public const string JwtAudience = "omnia-tests-clients";

    public static WebApplicationFactory<Program> Create(string connectionString, ILoggerProvider? loggerProvider = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", connectionString);
            builder.UseSetting("Jwt:Secret", JwtSecret);
            builder.UseSetting("Jwt:Issuer", JwtIssuer);
            builder.UseSetting("Jwt:Audience", JwtAudience);

            if (loggerProvider is not null)
            {
                builder.ConfigureLogging(logging => logging.AddProvider(loggerProvider));
            }
        });
    }
}
