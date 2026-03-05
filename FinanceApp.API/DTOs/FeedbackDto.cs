namespace FinanceApp.API.DTOs;

public record FeedbackDto(
    Guid Id,
    int Type,       // FeedbackType enum: 0=Question, 1=Suggestion, 2=Comment
    string? Subject,
    string Message,
    DateTimeOffset CreatedAt);

public record CreateFeedbackRequest(
    int Type,
    string Message,
    string? Subject = null);
