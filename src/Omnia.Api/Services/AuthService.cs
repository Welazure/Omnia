using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Omnia.Api.Auth;
using Omnia.Api.Data;
using Omnia.Shared.Contracts;

namespace Omnia.Api.Services;

public sealed class AuthService(
    OmniaDbContext database,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService tokenService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        if (await database.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = string.Empty,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        database.Users.Add(user);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (PostgresErrors.IsUniqueViolation(exception))
        {
            logger.LogWarning(exception, "Registration conflict for email {Email}", email);
            return null;
        }

        return new AuthResponse(tokenService.CreateToken(user), new UserDto(user.Id, user.Email));
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await database.Users.SingleOrDefaultAsync(candidate => candidate.Email == email, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return null;
        }

        return new AuthResponse(tokenService.CreateToken(user), new UserDto(user.Id, user.Email));
    }

    public async Task<UserDto?> GetUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await database.Users.FindAsync([userId], cancellationToken);
        return user is null ? null : new UserDto(user.Id, user.Email);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
