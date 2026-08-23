namespace FinanceApp.API.DTOs;

public class DeleteAccountRequest
{
    public string? CurrentPassword { get; set; }
    public string? ConfirmationPhrase { get; set; }
}
