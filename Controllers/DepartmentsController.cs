using Microsoft.AspNetCore.Mvc;
using QrDigitalLibrary.Api.Services;

namespace QrDigitalLibrary.Api.Controllers;

[ApiController]
[Route("api/departments")]
public sealed class DepartmentsController : ControllerBase
{
    private readonly IStudentLibraryService _studentLibraryService;

    public DepartmentsController(IStudentLibraryService studentLibraryService)
    {
        _studentLibraryService = studentLibraryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments(CancellationToken cancellationToken)
    {
        try
        {
            var departments = await _studentLibraryService.GetDepartmentsAsync(cancellationToken);
            return Ok(departments);
        }
        catch
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = "The library database could not be reached."
            });
        }
    }
}
