using FinanceApp.Domain.Common;

namespace FinanceApp.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; private set; }

    public string? Description { get; private set; }

    public ICollection<Expense> Expenses { get; private set; } = new List<Expense>();

    private Category() { Name = string.Empty; } // EF Core requires parameterless constructor

    public Category(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.");

        Name = name;
        Description = description;
    }

    public void UpdateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Category name cannot be empty.");

        Name = name;
    }

    public void UpdateDescription(string? description)
    {
        Description = description;
    }
}