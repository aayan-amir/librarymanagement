namespace QrDigitalLibrary.Api.Contracts;

public sealed class FinesResponse
{
    public required string UniversityId { get; init; }

    public required decimal OutstandingTotal { get; init; }

    public required IReadOnlyList<FineDto> Fines { get; init; }
}
