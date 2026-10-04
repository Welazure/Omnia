using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Omnia.Api.Data;

public sealed class OmniaDbContextFactory : IDesignTimeDbContextFactory<OmniaDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=localhost;Port=5432;Database=omnia;Username=omnia;Password=omnia";

    public OmniaDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OmniaDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        return new OmniaDbContext(options);
    }
}
