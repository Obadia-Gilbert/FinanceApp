using System.Linq.Expressions;
using FinanceApp.Application.Common;
using FinanceApp.Application.Extensions;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Application.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IRepository<UserFeedback> _repository;

    public FeedbackService(IRepository<UserFeedback> repository)
    {
        _repository = repository;
    }

    public async Task<UserFeedback> CreateAsync(string userId, FeedbackType type, string message, string? subject = null)
    {
        var feedback = new UserFeedback(userId, type, message, subject);
        await _repository.AddAsync(feedback);
        await _repository.SaveChangesAsync();
        return feedback;
    }

    public async Task<PagedResult<UserFeedback>> GetMyAsync(string userId, int pageNumber, int pageSize)
    {
        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter: f => f.UserId == userId,
            orderBy: q => q.OrderByDescending(f => f.CreatedAt));
    }

    public async Task<UserFeedback?> GetByIdAsync(Guid id, string userId, bool isAdmin = false)
    {
        var feedback = await _repository.GetByIdAsync(id);
        if (feedback == null) return null;
        if (!isAdmin && feedback.UserId != userId) return null;
        return feedback;
    }

    public async Task<PagedResult<UserFeedback>> GetPagedForAdminAsync(int pageNumber, int pageSize, FeedbackStatus? status = null, FeedbackType? type = null)
    {
        Expression<Func<UserFeedback, bool>>? filter = null;
        if (status.HasValue)
            filter = (filter ?? (f => true)).AndAlso(f => f.Status == status.Value);
        if (type.HasValue)
            filter = (filter ?? (f => true)).AndAlso(f => f.Type == type.Value);

        return await _repository.GetPagedAsync(
            pageNumber,
            pageSize,
            filter,
            orderBy: q => q.OrderByDescending(f => f.CreatedAt));
    }

    public async Task<bool> MarkAsReadAsync(Guid id)
    {
        var feedback = await _repository.GetByIdAsync(id);
        if (feedback == null) return false;
        feedback.SetStatus(FeedbackStatus.Read);
        _repository.Update(feedback);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetStatusAsync(Guid id, FeedbackStatus status)
    {
        var feedback = await _repository.GetByIdAsync(id);
        if (feedback == null) return false;
        feedback.SetStatus(status);
        _repository.Update(feedback);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetAdminNotesAsync(Guid id, string? notes)
    {
        var feedback = await _repository.GetByIdAsync(id);
        if (feedback == null) return false;
        feedback.SetAdminNotes(notes);
        _repository.Update(feedback);
        await _repository.SaveChangesAsync();
        return true;
    }
}
