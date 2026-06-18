namespace QrDigitalLibrary.Api.Contracts;

public sealed class RecommendationsResponse
{
    public required string UniversityId { get; init; }

    public required IReadOnlyList<RecommendationDto> Recommendations { get; init; }
}
