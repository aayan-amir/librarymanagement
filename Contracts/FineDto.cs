namespace QrDigitalLibrary.Api.Contracts;

public sealed class FineDto
{
    public required Guid FineId { get; init; }

    public required Guid TransactionId { get; init; }

    public required string Title { get; init; }

    public required string AccessionNo { get; init; }

    public required decimal Amount { get; init; }

    public required string Status { get; init; }

    public required DateTimeOffset AssessedAt { get; init; }
}
