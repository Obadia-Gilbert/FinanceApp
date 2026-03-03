using FinanceApp.Application.Common;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Moq;
using Xunit;

namespace FinanceApp.Tests.Services;

public class ExpenseServiceTests
{
    private readonly Mock<IRepository<Expense>> _repoMock;
    private readonly ExpenseService _sut;

    public ExpenseServiceTests()
    {
        _repoMock = new Mock<IRepository<Expense>>();
        _sut = new ExpenseService(_repoMock.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsExpense_WhenExists()
    {
        var id = Guid.NewGuid();
        var expense = new Expense(100, Currency.USD, DateTimeOffset.UtcNow, Guid.NewGuid(), "user-1", "Test");
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(expense);

        var result = await _sut.GetByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal(100, result.Amount);
        Assert.Equal(Currency.USD, result.Currency);
        _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Expense?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateExpenseAsync_AddsAndSaves_ReturnsExpense()
    {
        var categoryId = Guid.NewGuid();
        var userId = "user-1";
        var date = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Expense>())).Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        var result = await _sut.CreateExpenseAsync(50.00m, Currency.TZS, date, categoryId, userId, "Coffee", null);

        Assert.NotNull(result);
        Assert.Equal(50.00m, result.Amount);
        Assert.Equal(Currency.TZS, result.Currency);
        Assert.Equal(categoryId, result.CategoryId);
        Assert.Equal(userId, result.UserId);
        _repoMock.Verify(r => r.AddAsync(It.Is<Expense>(e => e.Amount == 50.00m && e.UserId == userId && e.CategoryId == categoryId)), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateExpenseAsync_Throws_WhenAmountZero()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.CreateExpenseAsync(0, Currency.USD, DateTime.UtcNow, Guid.NewGuid(), "user-1", "Test", null));
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Expense>()), Times.Never);
    }

    [Fact]
    public async Task SoftDeleteExpenseAsync_SoftDeletesAndSaves_WhenExists()
    {
        var id = Guid.NewGuid();
        var expense = new Expense(10, Currency.USD, DateTimeOffset.UtcNow, Guid.NewGuid(), "user-1", "To delete");
        _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(expense);
        _repoMock.Setup(r => r.SoftDelete(It.IsAny<Expense>())).Verifiable();
        _repoMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);

        await _sut.SoftDeleteExpenseAsync(id);

        _repoMock.Verify(r => r.GetByIdAsync(id), Times.Once);
        _repoMock.Verify(r => r.SoftDelete(expense), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteExpenseAsync_DoesNothing_WhenNotFound()
    {
        _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Expense?)null);

        await _sut.SoftDeleteExpenseAsync(Guid.NewGuid());

        _repoMock.Verify(r => r.SoftDelete(It.IsAny<Expense>()), Times.Never);
        _repoMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
