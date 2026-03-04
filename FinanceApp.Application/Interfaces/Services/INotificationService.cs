using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface INotificationService
{
    /// <summary>Creates a notification. Use topicKey to avoid duplicates (e.g. one per budget alert per month).</summary>
    Task<Notification> CreateAsync(string userId, string title, string message, NotificationType type, string? relatedLink = null, string? topicKey = null);

    /// <summary>Creates a notification only if no notification with the same topicKey exists for the user.</summary>
    Task<Notification?> CreateIfNotExistsAsync(string userId, string title, string message, NotificationType type, string? relatedLink = null, string? topicKey = null);

    Task<PagedResult<Notification>> GetByUserAsync(string userId, int pageNumber, int pageSize);

    Task<int> GetUnreadCountAsync(string userId);

    Task<bool> MarkAsReadAsync(Guid id, string userId);

    Task MarkAllAsReadAsync(string userId);

    Task<Notification?> GetByIdAsync(Guid id, string userId);
}
