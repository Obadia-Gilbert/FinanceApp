using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class FeedbackListViewModel
{
    public Guid Id { get; set; }
    public FeedbackType Type { get; set; }
    public string? Subject { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class FeedbackCreateViewModel
{
    [Required(ErrorMessage = "Please choose a type.")]
    [Display(Name = "Type")]
    public FeedbackType Type { get; set; }

    [Display(Name = "Subject (optional)")]
    [MaxLength(200)]
    public string? Subject { get; set; }

    [Required(ErrorMessage = "Message is required.")]
    [Display(Name = "Your message")]
    [MaxLength(4000)]
    [DataType(DataType.MultilineText)]
    public string Message { get; set; } = "";
}
