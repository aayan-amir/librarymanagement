using System.ComponentModel.DataAnnotations;

namespace QrDigitalLibrary.Api.Contracts;

public sealed class LoginStudentRequest
{
    [Required]
    public required string UniversityId { get; init; }

    [Required]
    public required string Password { get; init; }
}
