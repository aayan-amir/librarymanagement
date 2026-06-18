using System.ComponentModel.DataAnnotations;

namespace QrDigitalLibrary.Api.Contracts;

public sealed class ReturnBookRequest
{
    [Required]
    [StringLength(100)]
    public required string QrCodeId { get; init; }
}
