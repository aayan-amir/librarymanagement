namespace QrDigitalLibrary.Api.Contracts;

public sealed class AnalyticsDashboardResponse
{
    public required IReadOnlyList<DashboardListItemDto> MostActiveStudents { get; init; }

    public required IReadOnlyList<DashboardListItemDto> PopularBooks { get; init; }

    public required IReadOnlyList<DashboardListItemDto> PeakIssuingTimings { get; init; }

    public required IReadOnlyList<DashboardListItemDto> FineTrends { get; init; }

    public required IReadOnlyList<DashboardListItemDto> DepartmentWiseStatistics { get; init; }

    public required IReadOnlyList<DashboardListItemDto> DefaultersList { get; init; }

    public required IReadOnlyList<DashboardListItemDto> DailyTransactions { get; init; }
}
