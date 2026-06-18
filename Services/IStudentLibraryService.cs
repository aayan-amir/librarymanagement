using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public interface IStudentLibraryService
{
    Task<BorrowedBooksResponse> GetBorrowedBooksAsync(
        string universityId,
        CancellationToken cancellationToken);

    Task<TransactionHistoryResponse> GetTransactionHistoryAsync(
        string universityId,
        CancellationToken cancellationToken);

    Task<FinesResponse> GetFinesAsync(
        string universityId,
        CancellationToken cancellationToken);

    Task<bool> PayFineAsync(
        string universityId,
        Guid fineId,
        CancellationToken cancellationToken);

    Task<NotificationsResponse> GetNotificationsAsync(
        string universityId,
        CancellationToken cancellationToken);

    Task<RecommendationsResponse> GetRecommendationsAsync(
        string universityId,
        CancellationToken cancellationToken);

    Task<RegisterStudentResponse> RegisterStudentAsync(
        RegisterStudentRequest request,
        CancellationToken cancellationToken);

    Task<LoginStudentResponse> VerifyLoginAsync(
        LoginStudentRequest request,
        CancellationToken cancellationToken);

    Task<List<DepartmentDto>> GetDepartmentsAsync(
        CancellationToken cancellationToken);
}
