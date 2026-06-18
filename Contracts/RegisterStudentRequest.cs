using System.ComponentModel.DataAnnotations;

namespace QrDigitalLibrary.Api.Contracts;

public sealed class RegisterStudentRequest
{
    [Required]
    [StringLength(30, MinimumLength = 4)]
    public required string UniversityId { get; init; }

    [Required]
    [StringLength(150)]
    public required string FullName { get; init; }

    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    [MinLength(6)]
    public required string Password { get; init; }

    [Required]
    public required string DepartmentCode { get; init; }

    [Required]
    [Range(1, 8)]
    public required int Semester { get; init; }
}
