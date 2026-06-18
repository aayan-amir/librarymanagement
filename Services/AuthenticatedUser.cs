namespace QrDigitalLibrary.Api.Services;

public sealed class AuthenticatedUser
{
    public required string UniversityId { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }
}
