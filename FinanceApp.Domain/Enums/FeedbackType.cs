using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Domain.Enums;

/// <summary>Type of user feedback submission.</summary>
public enum FeedbackType
{
    [Display(Name = "Question")]
    Question = 0,

    [Display(Name = "Suggestion")]
    Suggestion = 1,

    [Display(Name = "Comment")]
    Comment = 2
}
