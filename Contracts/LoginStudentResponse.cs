namespace QrDigitalLibrary.Api.Contracts;

public sealed class LoginStudentResponse
{
    public required bool Success { get; init; }
    public required string StatusCode { get; init; }
    public required string Message { get; init; }
    public string? UniversityId { get; init; }
    public string? FullName { get; init; }
    public string? Role { get; init; }
    public string? AccessToken { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}
