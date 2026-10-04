using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Omnia.Api.Auth;

internal static class ClaimsPrincipalExtensions
{
    internal static Guid? GetUserId(this ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var userId) ? userId : null;
}
