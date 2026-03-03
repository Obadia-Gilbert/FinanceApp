using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Moq;
using Xunit;

namespace FinanceApp.Tests.Services;

public class CategoryServiceTests
{
    private readonly Mock<IRepository<Category>> _repoMock;
    private readonly CategoryService _sut;

    public CategoryServiceTests()
    {
        _repoMock = new Mock<IRepository<Category>>();
        _sut = new CategoryService(_repoMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCategory_WhenExistsAndUserOwns()
    {
        var id = Guid.NewGuid();
        var userId = "user-1";
        var category = new Category("Food", "Groceries", CategoryType.Expense) { UserId = userId };
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(id, userId);

        Assert.NotNull(result);
        Assert.Equal("Food", result.Name);
        Assert.Equal(userId, result.UserId);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotOwn()
    {
        var id = Guid.NewGuid();
        var category = new Category("Food", null, CategoryType.Expense) { UserId = "other-user" };
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(category);

        var result = await _sut.GetByIdAsync(id, "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Category?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid(), "user-1");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_Throws_WhenUserIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetByIdAsync(Guid.NewGuid(), ""));
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetByIdAsync(Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task GetAllAsync_ReturnsUserCategories()
    {
        var userId = "user-1";
        var categories = new List<Category>
        {
            new Category("Food", null, CategoryType.Expense) { UserId = userId },
            new Category("Salary", null, CategoryType.Income) { UserId = userId }
        };
        _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(categories);

        var result = await _sut.GetAllAsync(userId);

        Assert.Equal(2, result.Count());
        _repoMock.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()), Times.Once);
    }

    [Fact]
    public async Task GetCategoriesForExpenseAsync_ReturnsOnlyExpenseAndBoth()
    {
        var userId = "user-1";
        var categories = new List<Category>
        {
            new Category("Food", null, CategoryType.Expense) { UserId = userId },
            new Category("Mixed", null, CategoryType.Both) { UserId = userId }
        };
        _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(categories);

        var result = await _sut.GetCategoriesForExpenseAsync(userId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetCategoriesForIncomeAsync_ReturnsOnlyIncomeAndBoth()
    {
        var userId = "user-1";
        var categories = new List<Category>
        {
            new Category("Salary", null, CategoryType.Income) { UserId = userId },
            new Category("Mixed", null, CategoryType.Both) { UserId = userId }
        };
        _repoMock.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Category, bool>>>()))
            .ReturnsAsync(categories);

        var result = await _sut.GetCategoriesForIncomeAsync(userId);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task GetAllAsync_Throws_WhenUserIdEmpty()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _sut.GetAllAsync(""));
    }
}
