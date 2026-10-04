using Microsoft.EntityFrameworkCore;
using Omnia.Api.Auth;
using Omnia.Api.Data;

var builder = WebApplication.CreateBuilder(args);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
jwtOptions.Validate();

builder.Services.AddControllers();
builder.Services.AddDbContext<OmniaDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddOmniaAuth(jwtOptions);

var app = builder.Build();

ApplyMigrations(app);

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

static void ApplyMigrations(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        scope.ServiceProvider.GetRequiredService<OmniaDbContext>().Database.Migrate();
    }
    catch (Exception exception)
    {
        logger.LogError(exception, "Applying database migrations at startup failed.");
    }
}

public partial class Program;
