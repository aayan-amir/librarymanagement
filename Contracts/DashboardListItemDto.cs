namespace QrDigitalLibrary.Api.Contracts;

public sealed class DashboardListItemDto
{
    public required string Label { get; init; }

    public required string Detail { get; init; }

    public required decimal Value { get; init; }
}
