using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// In-app notification for the user (e.g. budget exceeded, category over limit).
/// </summary>
public class Notification : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Message { get; private set; } = null!;
    public NotificationType Type { get; private set; }
    public string? RelatedLink { get; private set; }
    /// <summary>When set, we avoid creating duplicate notifications for the same event (e.g. budget-2025-3).</summary>
    public string? TopicKey { get; private set; }
    public bool IsRead { get; private set; }

    protected Notification() { }

    public Notification(
        string userId,
        string title,
        string message,
        NotificationType type,
        string? relatedLink = null,
        string? topicKey = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        RelatedLink = relatedLink;
        TopicKey = topicKey;
        IsRead = false;
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
