using System.ComponentModel.DataAnnotations;
using FinanceApp.Domain.Enums;

namespace FinanceApp.Web.Models;

public class AccountViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public Currency Currency { get; set; }
    public decimal InitialBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}

public class AccountCreateViewModel
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public AccountType Type { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Initial balance cannot be negative.")]
    [Display(Name = "Initial Balance")]
    public decimal InitialBalance { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class AccountEditViewModel
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // read-only display
    public AccountType Type { get; set; }
    public Currency Currency { get; set; }
    public decimal CurrentBalance { get; set; }
}
