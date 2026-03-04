using FinanceApp.Application.Common;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Interfaces.Services;

public interface IFeedbackService
{
    /// <summary>Submit feedback as the current user.</summary>
    Task<UserFeedback> CreateAsync(string userId, FeedbackType type, string message, string? subject = null);

    /// <summary>Get current user's feedback (paginated).</summary>
    Task<PagedResult<UserFeedback>> GetMyAsync(string userId, int pageNumber, int pageSize);

    /// <summary>Get a single item; user can only get their own, admin can get any.</summary>
    Task<UserFeedback?> GetByIdAsync(Guid id, string userId, bool isAdmin = false);

    /// <summary>Admin: get all feedback with optional filters.</summary>
    Task<PagedResult<UserFeedback>> GetPagedForAdminAsync(int pageNumber, int pageSize, FeedbackStatus? status = null, FeedbackType? type = null);

    /// <summary>Admin: mark feedback as read.</summary>
    Task<bool> MarkAsReadAsync(Guid id);

    /// <summary>Admin: set status (e.g. Archived).</summary>
    Task<bool> SetStatusAsync(Guid id, FeedbackStatus status);

    /// <summary>Admin: set internal notes (not visible to user).</summary>
    Task<bool> SetAdminNotesAsync(Guid id, string? notes);
}
