using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardResponse> GetSummaryAsync(string? branchCode, CancellationToken cancellationToken);

    Task<AnalyticsDashboardResponse> GetAnalyticsAsync(string? branchCode, CancellationToken cancellationToken);

    Task<ActivityLogsResponse> GetActivityLogsAsync(int limit, CancellationToken cancellationToken);

    Task<bool> UpdateUserRoleAsync(string targetUniversityId, string newRole, string actorUniversityId, CancellationToken cancellationToken);
}
