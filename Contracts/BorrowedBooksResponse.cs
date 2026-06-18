namespace QrDigitalLibrary.Api.Contracts;

public sealed class BorrowedBooksResponse
{
    public required string UniversityId { get; init; }

    public required int ActiveBorrowCount { get; init; }

    public int BorrowLimit { get; init; } = 3;

    public required IReadOnlyList<BorrowedBookDto> Books { get; init; }
}
