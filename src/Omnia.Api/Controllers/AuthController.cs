using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnia.Api.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Api.Controllers;

[ApiController]
[Route("auth")]
[Authorize]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateCredentials(request.Email, request.Password))
        {
            return ValidationProblem(ModelState);
        }

        var response = await authService.RegisterAsync(request, cancellationToken);
        return response is null
            ? Conflict(EmailConflict())
            : StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        if (!ValidateCredentials(request.Email, request.Password))
        {
            return ValidationProblem(ModelState);
        }

        var response = await authService.LoginAsync(request, cancellationToken);
        return response is null ? Unauthorized(InvalidCredentials()) : Ok(response);
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId))
        {
            return Unauthorized();
        }

        var user = await authService.GetUserAsync(userId, cancellationToken);
        return user is null ? Unauthorized() : Ok(user);
    }

    private bool ValidateCredentials(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("email", "Email is required.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ModelState.AddModelError("password", "Password is required.");
        }

        return ModelState.IsValid;
    }

    private bool TryGetUserId(out Guid userId) =>
        Guid.TryParse(User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out userId);

    private static ProblemDetails EmailConflict() => new()
    {
        Title = "Email already registered.",
        Status = StatusCodes.Status409Conflict
    };

    private static ProblemDetails InvalidCredentials() => new()
    {
        Title = "Invalid email or password.",
        Status = StatusCodes.Status401Unauthorized
    };
}
