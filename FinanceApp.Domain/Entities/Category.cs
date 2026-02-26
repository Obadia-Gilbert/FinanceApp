using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; }

    public string? Description { get; private set; }

    // Navigation property for related Expenses
    public ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    // Parameterless constructor for EF Core
    private Category()
    {
        Name = string.Empty;
    }


    // Main constructor
    public Category(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.", nameof(name));

        Name = name;
        Description = description;
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

     public string UserId { get;  set; } = string.Empty;  // <-- new property for user association
}