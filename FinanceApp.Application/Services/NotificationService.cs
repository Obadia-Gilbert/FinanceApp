using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _repository;

    public NotificationService(IRepository<Notification> repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<Notification> CreateAsync(string userId, string title, string message, NotificationType type, string? relatedLink = null, string? topicKey = null)
    {
        var notification = new Notification(userId, title, message, type, relatedLink, topicKey);
        await _repository.AddAsync(notification);
        await _repository.SaveChangesAsync();
        return notification;
    }

    public async Task<Notification?> CreateIfNotExistsAsync(string userId, string title, string message, NotificationType type, string? relatedLink = null, string? topicKey = null)
    {
        if (!string.IsNullOrWhiteSpace(topicKey))
        {
            var existing = await _repository.FindAsync(n => n.UserId == userId && n.TopicKey == topicKey);
            if (existing.Any())
                return null;
        }
        var notif = new Notification(userId, title, message, type, relatedLink, topicKey);
        await _repository.AddAsync(notif);
        await _repository.SaveChangesAsync();
        return notif;
    }

    public async Task<PagedResult<Notification>> GetByUserAsync(string userId, int pageNumber, int pageSize)
    {
        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: n => n.UserId == userId,
            orderBy: q => q.OrderByDescending(n => n.CreatedAt));
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        var list = await _repository.FindAsync(n => n.UserId == userId && !n.IsRead);
        return list.Count();
    }

    public async Task<bool> MarkAsReadAsync(Guid id, string userId)
    {
        var notif = await _repository.GetByIdAsync(id);
        if (notif == null || notif.UserId != userId) return false;
        notif.MarkAsRead();
        _repository.Update(notif);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        var list = await _repository.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var n in list)
        {
            n.MarkAsRead();
            _repository.Update(n);
        }
        if (list.Any())
            await _repository.SaveChangesAsync();
    }

    public async Task<Notification?> GetByIdAsync(Guid id, string userId)
    {
        var notif = await _repository.GetByIdAsync(id);
        return notif != null && notif.UserId == userId ? notif : null;
    }
}
