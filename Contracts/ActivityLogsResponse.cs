namespace QrDigitalLibrary.Api.Contracts;

public sealed class ActivityLogsResponse
{
    public required IReadOnlyList<ActivityLogDto> Logs { get; init; }

    public required int TotalCount { get; init; }
}
