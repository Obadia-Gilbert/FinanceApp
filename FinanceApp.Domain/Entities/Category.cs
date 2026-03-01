using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; }

    public string? Description { get; private set; }

    // Icon name (e.g., "shopping-cart", "home", "car")
    public string? Icon { get; private set; } = "shopping-cart";

    // Badge color hex code (e.g., "#137fec")
    public string BadgeColor { get; private set; } = "#137fec";

    // User association
    public string UserId { get; set; } = string.Empty;

    // Navigation property for related Expenses
    public ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    // Parameterless constructor for EF Core
    private Category()
    {
        Name = string.Empty;
    }

    // Main constructor
    public Category(string name, string? description = null, string? icon = null, string? badgeColor = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
        Icon = icon ?? "shopping-cart";
        BadgeColor = badgeColor ?? "#137fec";
    }

    // Update methods
    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }

    public void UpdateIcon(string? icon)
    {
        Icon = icon ?? "shopping-cart";
    }

    public void UpdateBadgeColor(string badgeColor)
    {
        if (string.IsNullOrWhiteSpace(badgeColor) || !badgeColor.StartsWith("#") || badgeColor.Length != 7)
            throw new ArgumentException("Badge color must be a valid hex color (e.g., #137fec).", nameof(badgeColor));

        BadgeColor = badgeColor;
    }
}