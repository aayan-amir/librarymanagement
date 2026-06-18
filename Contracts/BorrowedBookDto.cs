namespace QrDigitalLibrary.Api.Contracts;

public sealed class BorrowedBookDto
{
    public required Guid TransactionId { get; init; }

    public required string Title { get; init; }

    public required string Author { get; init; }

    public required string AccessionNo { get; init; }

    public required DateTimeOffset IssuedAt { get; init; }

    public required DateTimeOffset DueAt { get; init; }

    public required string Status { get; init; }

    public bool IsOverdue { get; init; }

    public decimal EstimatedFine { get; init; }
}
