namespace QrDigitalLibrary.Api.Contracts;

public sealed class TransactionHistoryResponse
{
    public required string UniversityId { get; init; }

    public required IReadOnlyList<TransactionHistoryDto> Transactions { get; init; }
}
