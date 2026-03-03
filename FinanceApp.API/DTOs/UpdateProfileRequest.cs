using System.ComponentModel.DataAnnotations;

namespace FinanceApp.API.DTOs;

public class UpdateProfileRequest
{
    [StringLength(50)]
    public string? FirstName { get; set; }

    [StringLength(50)]
    public string? LastName { get; set; }

    [StringLength(20)]
    [Phone]
    public string? PhoneNumber { get; set; }

    [StringLength(10)]
    public string? CountryCode { get; set; }
}
