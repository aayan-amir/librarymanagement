namespace QrDigitalLibrary.Api.Contracts;

public sealed class ActivityLogDto
{
    public required Guid ActivityLogId { get; init; }

    public string? ActorUniversityId { get; init; }

    public string? ActorRole { get; init; }

    public required string ActionType { get; init; }

    public required string ActionStatus { get; init; }

    public string? Endpoint { get; init; }

    public string? FailureReason { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
