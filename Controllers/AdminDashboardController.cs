using Microsoft.AspNetCore.Mvc;
using QrDigitalLibrary.Api.Contracts;
using QrDigitalLibrary.Api.Services;
using System.Security.Claims;

namespace QrDigitalLibrary.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AdminDashboardResponse>> GetSummary(
        [FromQuery] string? branchCode,
        CancellationToken cancellationToken)
    {
        if (!IsDashboardUser())
        {
            return ForbidOrUnauthorized();
        }

        try
        {
            return Ok(await _dashboardService.GetSummaryAsync(branchCode, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_NOT_CONFIGURED",
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_CONNECTION_FAILED",
                message = "The library database could not be reached."
            });
        }
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<AnalyticsDashboardResponse>> GetAnalytics(
        [FromQuery] string? branchCode,
        CancellationToken cancellationToken)
    {
        if (!IsDashboardUser())
        {
            return ForbidOrUnauthorized();
        }

        try
        {
            return Ok(await _dashboardService.GetAnalyticsAsync(branchCode, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_NOT_CONFIGURED",
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_CONNECTION_FAILED",
                message = "The library database could not be reached."
            });
        }
    }

    [HttpGet("logs")]
    public async Task<ActionResult<ActivityLogsResponse>> GetLogs(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!IsDashboardUser())
        {
            return ForbidOrUnauthorized();
        }

        try
        {
            return Ok(await _dashboardService.GetActivityLogsAsync(Math.Clamp(limit, 1, 200), cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_NOT_CONFIGURED",
                message = ex.Message
            });
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                statusCode = "DATABASE_CONNECTION_FAILED",
                message = "The library database could not be reached."
            });
        }
    }

    [HttpPut("roles")]
    public async Task<ActionResult> UpdateRole(
        [FromBody] RoleUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsDashboardUser())
        {
            return ForbidOrUnauthorized();
        }

        var actorId = User.FindFirstValue("university_id");
        if (actorId == null) return Unauthorized();

        try
        {
            var success = await _dashboardService.UpdateUserRoleAsync(request.UniversityId, request.NewRole, actorId, cancellationToken);
            if (success)
            {
                return Ok(new { message = "User role updated successfully." });
            }
            return BadRequest(new { message = "User not found or role update failed." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    private bool IsDashboardUser()
    {
        var role = User.FindFirstValue(ClaimTypes.Role);
        return role is "Admin" or "Librarian";
    }

    private ActionResult ForbidOrUnauthorized() =>
        User.Identity?.IsAuthenticated == true ? Forbid() : Unauthorized();
}
