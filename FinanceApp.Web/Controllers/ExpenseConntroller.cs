using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using FinanceApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Web.Models;

namespace FinanceApp.Web.Controllers;

public class ExpenseController : Controller
{
    private readonly IRepository<Expense> _expenseRepository;
    private readonly IRepository<Category> _categoryRepository;

    public ExpenseController(
        IRepository<Expense> expenseRepository,
        IRepository<Category> categoryRepository)
    {
        _expenseRepository = expenseRepository;
        _categoryRepository = categoryRepository;
    }

    // GET: /Expense
    public async Task<IActionResult> Index()
{
    var expenses = await _expenseRepository.GetAllAsync(e => e.Category);
    return View(expenses);
}

    // GET: /Expense/Create
    public async Task<IActionResult> Create()
    {
        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        return View();
    }

    // POST: /Expense/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExpenseViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));
        return View(model);
    }

    // Example: get UserId from logged-in user
    var userId = Guid.NewGuid(); // TODO: replace with actual user ID

    var expense = new Expense(
        amount: model.Amount,
        currency: model.Currency,
        expenseDate: model.ExpenseDate,
        categoryId: model.CategoryId,
        userId: userId,
        description: model.Description,
        receiptPath: model.ReceiptPath
    );

    await _expenseRepository.AddAsync(expense);
    await _expenseRepository.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}

    // GET: /Expense/Edit/{id}
    public async Task<IActionResult> Edit(Guid id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();

        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        return View(expense);
    }

    // POST: /Expense/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, Expense updatedExpense)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();

        // Update fields
        expense.UpdateDescription(updatedExpense.Description ?? expense.Description);
        expense.UpdateReceipt(updatedExpense.ReceiptPath ?? expense.ReceiptPath);

        _expenseRepository.Update(expense);
        await _expenseRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: /Expense/Delete/{id}
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        return View(expense);
    }

    // POST: /Expense/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();

        _expenseRepository.SoftDelete(expense);
        await _expenseRepository.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}


/*using FinanceApp.Application.Interfaces;
using FinanceApp.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpenseController : ControllerBase
{
    private readonly IRepository<Expense> _expenseRepository;

    public ExpenseController(IRepository<Expense> expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    // GET: api/expense
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var expenses = await _expenseRepository.GetAllAsync();
        return Ok(expenses);
    }

    // GET: api/expense/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    // POST: api/expense
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Expense expense)
    {
        if (expense == null) return BadRequest();

        await _expenseRepository.AddAsync(expense);
        await _expenseRepository.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = expense.Id }, expense);
    }

    // PUT: api/expense/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] Expense updatedExpense)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();

        // Example: updating Description & ReceiptPath only
        expense.UpdateDescription(updatedExpense.Description ?? expense.Description);
        expense.UpdateReceipt(updatedExpense.ReceiptPath ?? expense.ReceiptPath);

        _expenseRepository.Update(expense);
        await _expenseRepository.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/expense/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var expense = await _expenseRepository.GetByIdAsync(id);
        if (expense == null) return NotFound();

        _expenseRepository.SoftDelete(expense);
        await _expenseRepository.SaveChangesAsync();

        return NoContent();
    }
}*/