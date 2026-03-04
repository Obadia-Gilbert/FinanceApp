using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Domain.Enums;

/// <summary>Admin-managed status for feedback items.</summary>
public enum FeedbackStatus
{
    [Display(Name = "New")]
    New = 0,

    [Display(Name = "Read")]
    Read = 1,

    [Display(Name = "Archived")]
    Archived = 2
}
