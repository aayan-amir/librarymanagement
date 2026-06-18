using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QrDigitalLibrary.Api.Contracts;
using QrDigitalLibrary.Api.Services;

namespace QrDigitalLibrary.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IStudentLibraryService _studentLibraryService;

    public AuthController(IStudentLibraryService studentLibraryService)
    {
        _studentLibraryService = studentLibraryService;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<RegisterStudentResponse>> Register(
        [FromBody] RegisterStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentLibraryService.RegisterStudentAsync(request, cancellationToken);
            return result.Success ? Ok(result) : Conflict(result);
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new RegisterStudentResponse
            {
                Success = false,
                StatusCode = "DATABASE_CONNECTION_FAILED",
                Message = "The library database could not be reached."
            });
        }
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginStudentResponse>> Login(
        [FromBody] LoginStudentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _studentLibraryService.VerifyLoginAsync(request, cancellationToken);
            if (result.Success)
                return Ok(result);

            return result.StatusCode == "STUDENT_INACTIVE"
                ? StatusCode(StatusCodes.Status403Forbidden, result)
                : Unauthorized(result);
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new LoginStudentResponse
            {
                Success = false,
                StatusCode = "DATABASE_CONNECTION_FAILED",
                Message = "The library database could not be reached."
            });
        }
    }
}
