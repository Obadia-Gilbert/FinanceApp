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
    public async Task<IActionResult> Create(ExpenseCreateViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));
        return View(model);
    }

    // Handle file upload
    string? receiptPath = null;
    if (model.ReceiptFile != null && model.ReceiptFile.Length > 0)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/receipts");
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        // Generate descriptive filename
        // Example: Expense-Food-2026-02-17-<expenseId>.jpg
        var category = await _categoryRepository.GetByIdAsync(model.CategoryId);
        var categoryName = category?.Name.Replace(" ", "-") ?? "Unknown";
        var dateString = model.ExpenseDate.ToString("yyyy-MM-dd");
    
        // If creating new expense, generate a GUID for filename
        var expenseId = Guid.NewGuid(); // will assign to expense.Id later
        var fileExtension = Path.GetExtension(model.ReceiptFile.FileName);
        var fileName = $"Expense-{categoryName}-{dateString}-{expenseId}{fileExtension}";

        //var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.ReceiptFile.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        // Save the file to disk
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
        await model.ReceiptFile.CopyToAsync(fileStream);
        }

        receiptPath = $"/uploads/receipts/{fileName}";
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
        receiptPath: receiptPath
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

    var model = new ExpenseEditViewModel
    {
        Id = expense.Id,
        Amount = expense.Amount,
        Currency = expense.Currency,
        ExpenseDate = expense.ExpenseDate,
        CategoryId = expense.CategoryId,
        Description = expense.Description,
        ReceiptPath = expense.ReceiptPath
    };

    ViewBag.Categories = await _categoryRepository.GetAllAsync();
    ViewBag.Currencies = Enum.GetValues(typeof(Currency));

    return View(model);
}

    // POST: /Expense/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
public async Task<IActionResult> Edit(ExpenseEditViewModel model)
{
    if (!ModelState.IsValid)
    {
        ViewBag.Categories = await _categoryRepository.GetAllAsync();
        ViewBag.Currencies = Enum.GetValues(typeof(Currency));
        return View(model);
    }

    var expense = await _expenseRepository.GetByIdAsync(model.Id);
    if (expense == null) return NotFound();

    // Update via domain logic (clean way)
    expense.UpdateDescription(model.Description ?? "");
    expense.UpdateReceipt(model.ReceiptPath ?? "");
    expense.UpdateCategory(model.CategoryId);
    expense.UpdateCurrency(model.Currency);
    expense.UpdateExpenseDate(model.ExpenseDate);
    expense.UpdateAmount(model.Amount);

    // If you want to allow updating amount/currency/date,
    // we should add domain update methods instead of setting directly.

    _expenseRepository.Update(expense);
    await _expenseRepository.SaveChangesAsync();

    return RedirectToAction(nameof(Index));
}

    // GET: /Expense/Delete/{id}
   public async Task<IActionResult> Delete(Guid id)
{
     var expenses = await _expenseRepository.GetAllAsync(e => e.Category);
     var expense = expenses.FirstOrDefault(e => e.Id == id);

    if (expense == null)
        return NotFound();

    var vm = new ExpenseDeleteViewModel
    {
        Id = expense.Id,
        Description = expense.Description,
        Currency = expense.Currency,
        CategoryName = expense.Category.Name,
        Amount = expense.Amount,
        ExpenseDate = expense.ExpenseDate
        
        
    };

    return View(vm); // ✅ Now correct type
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