using System.ComponentModel.DataAnnotations;

namespace QrDigitalLibrary.Api.Contracts;

public class RoleUpdateRequest
{
    [Required]
    public string UniversityId { get; set; } = string.Empty;

    [Required]
    public string NewRole { get; set; } = string.Empty;
}
