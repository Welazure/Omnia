using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Omnia.Api.Auth;
using Omnia.Api.Services;
using Omnia.Shared.Contracts;

namespace Omnia.Api.Controllers;

[ApiController]
[Route("clips")]
[Authorize]
public sealed class ClipsController(IClipService clipService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClipDto>>> GetClips(CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        var clips = await clipService.GetClipsAsync(userId, cancellationToken);
        return Ok(clips);
    }

    [HttpPost]
    public async Task<ActionResult<ClipDto>> CreateClip(CreateClipRequest request, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        if (!ValidateContent(request.Content))
        {
            return ValidationProblem(ModelState);
        }

        var clip = await clipService.CreateAsync(userId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, clip);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteClip(Guid id, CancellationToken cancellationToken)
    {
        if (User.GetUserId() is not { } userId)
        {
            return Unauthorized();
        }

        if (!await clipService.DeleteAsync(userId, id, cancellationToken))
        {
            return NotFound(new ProblemDetails
            {
                Title = "Clip not found.",
                Status = StatusCodes.Status404NotFound
            });
        }

        return NoContent();
    }

    private bool ValidateContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            ModelState.AddModelError(nameof(CreateClipRequest.Content), "Content is required.");
        }

        return ModelState.IsValid;
    }
}
