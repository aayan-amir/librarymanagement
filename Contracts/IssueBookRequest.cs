using System.ComponentModel.DataAnnotations;

namespace QrDigitalLibrary.Api.Contracts;

public sealed class IssueBookRequest
{
    [Required]
    [StringLength(30, MinimumLength = 4)]
    public required string UniversityId { get; init; }

    [Required]
    [StringLength(100)]
    public required string QrCodeId { get; init; }
}
