namespace QrDigitalLibrary.Api.Contracts;

public sealed class AdminDashboardResponse
{
    public required int TotalIssuedBooks { get; init; }

    public required int OverdueBooks { get; init; }

    public required int ActiveBorrowers { get; init; }

    public required int ActiveInventory { get; init; }

    public required decimal OutstandingFines { get; init; }

    public required decimal FineCollectionTotal { get; init; }
}
