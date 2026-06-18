namespace QrDigitalLibrary.Api.Contracts;

public sealed class ReturnBookResponse
{
    public required bool Success { get; init; }

    public Guid? TransactionId { get; init; }

    public required string StatusCode { get; init; }

    public required string Message { get; init; }

    public DateTimeOffset? ReturnedAt { get; init; }

    public decimal FineAmount { get; init; }

    public DateTimeOffset ServerTimeUtc { get; init; } = DateTimeOffset.UtcNow;
}
