using Microsoft.AspNetCore.Mvc;
using QrDigitalLibrary.Api.Contracts;
using QrDigitalLibrary.Api.Services;
using System.Security.Claims;

namespace QrDigitalLibrary.Api.Controllers;

[ApiController]
[Route("api/students")]
public sealed class StudentsController : ControllerBase
{
    private readonly IStudentLibraryService _studentLibraryService;

    public StudentsController(IStudentLibraryService studentLibraryService)
    {
        _studentLibraryService = studentLibraryService;
    }

    [HttpGet("me/borrowed-books")]
    public Task<ActionResult<BorrowedBooksResponse>> GetMyBorrowedBooks(CancellationToken cancellationToken)
    {
        var universityId = RequireUniversityId();
        return universityId is null
            ? Task.FromResult<ActionResult<BorrowedBooksResponse>>(Unauthorized())
            : GetBorrowedBooks(universityId, cancellationToken);
    }

    [HttpGet("me/transactions")]
    public Task<ActionResult<TransactionHistoryResponse>> GetMyTransactions(CancellationToken cancellationToken)
    {
        var universityId = RequireUniversityId();
        return universityId is null
            ? Task.FromResult<ActionResult<TransactionHistoryResponse>>(Unauthorized())
            : GetTransactions(universityId, cancellationToken);
    }

    [HttpGet("me/fines")]
    public async Task<ActionResult<FinesResponse>> GetMyFines(CancellationToken cancellationToken)
    {
        var universityId = RequireUniversityId();
        if (universityId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _studentLibraryService.GetFinesAsync(universityId, cancellationToken));
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

    [HttpGet("me/notifications")]
    public async Task<ActionResult<NotificationsResponse>> GetMyNotifications(CancellationToken cancellationToken)
    {
        var universityId = RequireUniversityId();
        if (universityId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _studentLibraryService.GetNotificationsAsync(universityId, cancellationToken));
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

    [HttpGet("me/recommendations")]
    public async Task<ActionResult<RecommendationsResponse>> GetMyRecommendations(CancellationToken cancellationToken)
    {
        var universityId = RequireUniversityId();
        if (universityId is null)
        {
            return Unauthorized();
        }

        try
        {
            return Ok(await _studentLibraryService.GetRecommendationsAsync(universityId, cancellationToken));
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

    [HttpGet("{universityId}/borrowed-books")]
    [ProducesResponseType(typeof(BorrowedBooksResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<BorrowedBooksResponse>> GetBorrowedBooks(
        string universityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _studentLibraryService.GetBorrowedBooksAsync(universityId, cancellationToken);
            return Ok(response);
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

    [HttpGet("{universityId}/transactions")]
    [ProducesResponseType(typeof(TransactionHistoryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<TransactionHistoryResponse>> GetTransactions(
        string universityId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _studentLibraryService.GetTransactionHistoryAsync(universityId, cancellationToken);
            return Ok(response);
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

    [HttpGet("{universityId}/fines")]
    public async Task<ActionResult<FinesResponse>> GetFines(
        string universityId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _studentLibraryService.GetFinesAsync(universityId, cancellationToken));
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

    [HttpPost("me/fines/{fineId}/pay")]
    public async Task<ActionResult> PayFine(
        Guid fineId,
        CancellationToken cancellationToken)
    {
        var uid = RequireUniversityId();
        if (uid == null)
            return Unauthorized();

        try
        {
            var success = await _studentLibraryService.PayFineAsync(uid, fineId, cancellationToken);
            if (success)
            {
                return Ok(new { message = "Fine paid successfully." });
            }
            return BadRequest(new { message = "Fine could not be paid (not found or already paid)." });
        }
        catch (Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    [HttpGet("{universityId}/notifications")]
    public async Task<ActionResult<NotificationsResponse>> GetNotifications(
        string universityId,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _studentLibraryService.GetNotificationsAsync(universityId, cancellationToken));
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

    private string? RequireUniversityId() => User.FindFirstValue("university_id");
}
