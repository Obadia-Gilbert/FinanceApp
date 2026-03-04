using FinanceApp.Domain.Common;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Domain.Entities;

/// <summary>
/// User-submitted feedback: questions, suggestions, or comments. Visible to admins for support and product improvement.
/// </summary>
public class UserFeedback : BaseEntity
{
    public string UserId { get; private set; } = null!;
    public FeedbackType Type { get; private set; }
    /// <summary>Optional short title (e.g. for questions/suggestions).</summary>
    public string? Subject { get; private set; }
    public string Message { get; private set; } = null!;
    public FeedbackStatus Status { get; private set; }
    /// <summary>Internal notes from admin; not shown to the user.</summary>
    public string? AdminNotes { get; private set; }

    protected UserFeedback() { }

    public UserFeedback(
        string userId,
        FeedbackType type,
        string message,
        string? subject = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("UserId is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Message is required.", nameof(message));

        UserId = userId;
        Type = type;
        Message = message.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Status = FeedbackStatus.New;
    }

    public void SetStatus(FeedbackStatus status) => Status = status;
    public void SetAdminNotes(string? notes) => AdminNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
}
