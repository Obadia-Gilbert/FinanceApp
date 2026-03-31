namespace FinanceApp.Web.Helpers;

/// <summary>
/// Common countries with ISO 3166-1 alpha-2 code and display name for profile/phone.
/// </summary>
public static class CountryList
{
    /// <summary>ISO 3166-1 alpha-2 code -> Country name. Use for dropdowns and lookups.</summary>
    public static IReadOnlyList<(string Code, string Name)> All { get; } = new List<(string, string)>
    {
        ("", "— Select country —"),
        ("TZ", "Tanzania"),
        ("UG", "Uganda"),
        ("KE", "Kenya"),
        ("RW", "Rwanda"),
        ("US", "United States"),
        ("GB", "United Kingdom"),
        ("CA", "Canada"),
        ("AU", "Australia"),
        ("DE", "Germany"),
        ("FR", "France"),
        ("ES", "Spain"),
        ("IN", "India"),
        ("ZA", "South Africa"),
        ("NG", "Nigeria"),
        ("GH", "Ghana"),
        ("ET", "Ethiopia"),
        ("CN", "China"),
        ("JP", "Japan"),
        ("BR", "Brazil"),
        ("MX", "Mexico"),
        ("NL", "Netherlands"),
        ("CH", "Switzerland"),
        ("AE", "United Arab Emirates"),
        ("SA", "Saudi Arabia"),
        ("EG", "Egypt"),
        ("ZW", "Zimbabwe"),
        ("ZM", "Zambia"),
        ("MW", "Malawi"),
        ("MZ", "Mozambique"),
        ("BW", "Botswana"),
        ("NA", "Namibia"),
    };

    public static string? GetNameByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var pair = All.FirstOrDefault(c => string.Equals(c.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        return pair.Code == null ? null : pair.Name;
    }
}
