namespace QrDigitalLibrary.Api.Contracts;

public sealed class NotificationsResponse
{
    public required string UniversityId { get; init; }

    public required IReadOnlyList<NotificationDto> Notifications { get; init; }
}
