namespace FinanceApp.API.Helpers;

/// <summary>Country code to name resolution for profile (mirrors Web CountryList).</summary>
public static class CountryHelper
{
    private static readonly IReadOnlyList<(string Code, string Name)> All = new List<(string, string)>
    {
        ("", "— Select country —"),
        ("TZ", "Tanzania"), ("UG", "Uganda"), ("KE", "Kenya"), ("RW", "Rwanda"),
        ("US", "United States"), ("GB", "United Kingdom"), ("CA", "Canada"), ("AU", "Australia"),
        ("DE", "Germany"), ("FR", "France"), ("IN", "India"), ("ZA", "South Africa"),
        ("NG", "Nigeria"), ("GH", "Ghana"), ("ET", "Ethiopia"), ("CN", "China"), ("JP", "Japan"),
        ("BR", "Brazil"), ("MX", "Mexico"), ("NL", "Netherlands"), ("CH", "Switzerland"),
        ("AE", "United Arab Emirates"), ("SA", "Saudi Arabia"), ("EG", "Egypt"),
        ("ZW", "Zimbabwe"), ("ZM", "Zambia"), ("MW", "Malawi"), ("MZ", "Mozambique"),
        ("BW", "Botswana"), ("NA", "Namibia"),
        ("ES", "Spain"),
    };

    public static string? GetNameByCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var pair = All.FirstOrDefault(c => string.Equals(c.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
        return pair.Code == null ? null : pair.Name;
    }
}
