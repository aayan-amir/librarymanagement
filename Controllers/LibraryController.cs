using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using QrDigitalLibrary.Api.Contracts;
using QrDigitalLibrary.Api.Services;
using System.Security.Claims;

namespace QrDigitalLibrary.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class LibraryController : ControllerBase
{
    private readonly ILibraryIssueService _libraryIssueService;

    public LibraryController(ILibraryIssueService libraryIssueService)
    {
        _libraryIssueService = libraryIssueService;
    }

    [HttpPost("issue")]
    [EnableRateLimiting("transactions")]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(IssueBookResponse), StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IssueBookResponse>> IssueBookViaQr(
        [FromBody] IssueBookRequest request,
        CancellationToken cancellationToken)
    {
        IssueBookDbResult result;

        try
        {
            result = await _libraryIssueService.IssueBookViaQrAsync(request, cancellationToken);
        }
        catch
        {
            result = new IssueBookDbResult
            {
                Success = false,
                TransactionId = null,
                StatusCode = "DATABASE_CONNECTION_FAILED",
                Message = "The library database could not be reached.",
                IssuedAt = null,
                DueAt = null
            };
        }

        var response = new IssueBookResponse
        {
            Success = result.Success,
            TransactionId = result.TransactionId,
            StatusCode = result.StatusCode,
            Message = result.Message,
            IssuedAt = result.IssuedAt,
            DueAt = result.DueAt
        };

        if (result.Success)
        {
            return Ok(response);
        }

        return result.StatusCode switch
        {
            "STUDENT_NOT_FOUND" or "QR_CODE_NOT_FOUND" => NotFound(response),
            "STUDENT_INACTIVE" => StatusCode(StatusCodes.Status403Forbidden, response),
            "BORROW_LIMIT_REACHED" or "COPY_NOT_AVAILABLE" => Conflict(response),
            "DATABASE_NOT_CONFIGURED" or "DATABASE_CONNECTION_FAILED" => StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            _ => BadRequest(response)
        };
    }

    [HttpPost("return")]
    [EnableRateLimiting("transactions")]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ReturnBookResponse), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReturnBookResponse>> ReturnBookViaQr(
        [FromBody] ReturnBookRequest request,
        CancellationToken cancellationToken)
    {
        var universityId = User.FindFirstValue("university_id");
        if (string.IsNullOrWhiteSpace(universityId))
        {
            return Unauthorized(new ReturnBookResponse
            {
                Success = false,
                StatusCode = "AUTH_REQUIRED",
                Message = "Login is required before returning a book."
            });
        }

        ReturnBookDbResult result;

        try
        {
            result = await _libraryIssueService.ReturnBookViaQrAsync(universityId, request, cancellationToken);
        }
        catch
        {
            result = new ReturnBookDbResult
            {
                Success = false,
                TransactionId = null,
                StatusCode = "DATABASE_CONNECTION_FAILED",
                Message = "The library database could not be reached.",
                ReturnedAt = null,
                FineAmount = 0
            };
        }

        var response = new ReturnBookResponse
        {
            Success = result.Success,
            TransactionId = result.TransactionId,
            StatusCode = result.StatusCode,
            Message = result.Message,
            ReturnedAt = result.ReturnedAt,
            FineAmount = result.FineAmount
        };

        if (result.Success)
        {
            return Ok(response);
        }

        return result.StatusCode switch
        {
            "STUDENT_NOT_FOUND" or "QR_CODE_NOT_FOUND" => NotFound(response),
            "NO_ACTIVE_ISSUE" or "COPY_NOT_ISSUED_TO_STUDENT" => Conflict(response),
            "DATABASE_NOT_CONFIGURED" or "DATABASE_CONNECTION_FAILED" => StatusCode(StatusCodes.Status503ServiceUnavailable, response),
            _ => BadRequest(response)
        };
    }
}
