namespace QrDigitalLibrary.Api.Contracts;

public sealed class RecommendationDto
{
    public required int Rank { get; init; }

    public required Guid BookId { get; init; }

    public required string Title { get; init; }

    public required string Author { get; init; }

    public string? Category { get; init; }

    public required int AvailabilityCount { get; init; }

    public required int Score { get; init; }

    public required string Reason { get; init; }
}
