namespace FinanceApp.Localization;

public static class SupportedLanguages
{
    public static readonly string[] Codes = ["en", "sw", "es"];

    public static string Normalize(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return "en";
        var c = code.Trim().ToLowerInvariant();
        return Codes.Contains(c) ? c : "en";
    }
}
