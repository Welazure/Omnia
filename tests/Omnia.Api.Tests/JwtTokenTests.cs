using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Omnia.Api.Auth;
using Omnia.Api.Data;

namespace Omnia.Api.Tests;

public class JwtTokenTests
{
    private static readonly JwtOptions Options = new()
    {
        Secret = ApiFactory.JwtSecret,
        Issuer = ApiFactory.JwtIssuer,
        Audience = ApiFactory.JwtAudience
    };

    [Fact]
    public void CreateToken_IncludesSubAndEmailClaims()
    {
        var user = NewUser();
        var token = new JwtTokenService(Options).CreateToken(user);

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), parsed.Claims.Single(claim => claim.Type == "sub").Value);
        Assert.Equal(user.Email, parsed.Claims.Single(claim => claim.Type == "email").Value);
    }

    [Fact]
    public void CreateToken_UsesConfiguredIssuerAndAudience()
    {
        var token = new JwtTokenService(Options).CreateToken(NewUser());

        var parsed = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Options.Issuer, parsed.Issuer);
        Assert.Contains(Options.Audience, parsed.Audiences);
    }

    [Fact]
    public void CreateToken_IsSignedWithConfiguredSecret()
    {
        var token = new JwtTokenService(Options).CreateToken(NewUser());
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Options.Issuer,
            ValidateAudience = true,
            ValidAudience = Options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret)),
            ValidateLifetime = true
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);

        Assert.NotNull(principal);
    }

    [Fact]
    public void CreateToken_WrongSecret_FailsValidation()
    {
        var token = new JwtTokenService(Options).CreateToken(NewUser());
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Options.Issuer,
            ValidateAudience = true,
            ValidAudience = Options.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-totally-different-secret-0123456789ab")),
            ValidateLifetime = true
        };

        Assert.ThrowsAny<SecurityTokenException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _));
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = "claims@example.com",
        PasswordHash = "hash",
        CreatedAt = DateTimeOffset.UtcNow
    };
}
