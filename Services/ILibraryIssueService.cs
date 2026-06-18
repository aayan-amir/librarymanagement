using QrDigitalLibrary.Api.Contracts;

namespace QrDigitalLibrary.Api.Services;

public interface ILibraryIssueService
{
    Task<IssueBookDbResult> IssueBookViaQrAsync(
        IssueBookRequest request,
        CancellationToken cancellationToken);

    Task<ReturnBookDbResult> ReturnBookViaQrAsync(
        string universityId,
        ReturnBookRequest request,
        CancellationToken cancellationToken);
}
