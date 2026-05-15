namespace FinanceApp.API.DTOs;

public class ExternalLoginRequest
{
    /// <summary>google | facebook</summary>
    public string Provider { get; set; } = "";

    public string? IdToken { get; set; }
    public string? AccessToken { get; set; }
}
