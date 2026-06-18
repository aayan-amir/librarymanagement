namespace QrDigitalLibrary.Api.Contracts;

public sealed class RegisterStudentResponse
{
    public required bool Success { get; init; }
    public required string StatusCode { get; init; }
    public required string Message { get; init; }
}
