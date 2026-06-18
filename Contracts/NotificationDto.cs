namespace QrDigitalLibrary.Api.Contracts;

public sealed class NotificationDto
{
    public required Guid NotificationId { get; init; }

    public required string NotificationType { get; init; }

    public required string Title { get; init; }

    public required string Message { get; init; }

    public required bool IsRead { get; init; }

    public required DateTimeOffset CreatedAt { get; init; }
}
