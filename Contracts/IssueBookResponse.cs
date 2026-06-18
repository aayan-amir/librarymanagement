namespace QrDigitalLibrary.Api.Contracts;

public sealed class IssueBookResponse
{
    public required bool Success { get; init; }

    public Guid? TransactionId { get; init; }

    public required string StatusCode { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset? IssuedAt { get; init; }

    public DateTimeOffset? DueAt { get; init; }

    public DateTimeOffset ServerTimeUtc { get; init; } = DateTimeOffset.UtcNow;
}
