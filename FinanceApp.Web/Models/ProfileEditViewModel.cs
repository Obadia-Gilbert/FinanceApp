using System.ComponentModel.DataAnnotations;

namespace FinanceApp.Web.Models;

public class ProfileEditViewModel
{
    [StringLength(50)]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [StringLength(50)]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [Phone]
    [StringLength(20)]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    [StringLength(100)]
    [Display(Name = "Country")]
    public string? Country { get; set; }

    [StringLength(10)]
    [Display(Name = "Country Code")]
    public string? CountryCode { get; set; }

    [Display(Name = "Profile Photo")]
    public IFormFile? ProfileImage { get; set; }

    /// <summary>Current profile image path for display (read-only).</summary>
    public string? CurrentProfileImagePath { get; set; }

    public string? Email { get; set; }
}
