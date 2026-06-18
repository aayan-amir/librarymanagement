namespace QrDigitalLibrary.Api.Services;

public sealed class IssueBookDbResult
{
    public required bool Success { get; init; }

    public Guid? TransactionId { get; init; }

    public required string StatusCode { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset? IssuedAt { get; init; }

    public DateTimeOffset? DueAt { get; init; }
}
