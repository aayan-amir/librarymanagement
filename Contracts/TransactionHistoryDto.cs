namespace QrDigitalLibrary.Api.Contracts;

public sealed class TransactionHistoryDto
{
    public required Guid TransactionId { get; init; }

    public required string Title { get; init; }

    public required string Author { get; init; }

    public required string AccessionNo { get; init; }

    public required string TransactionType { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }

    public required DateTimeOffset DueAt { get; init; }

    public DateTimeOffset? ReturnedAt { get; init; }

    public decimal FineAmount { get; init; }
}
