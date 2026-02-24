using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Domain.Entities;
using FinanceApp.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinanceApp.Infrastructure.Identity; // for ApplicationUser

namespace FinanceApp.Web.Controllers
{
    [Authorize] // Only authenticated users can access
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CategoryController(ICategoryService categoryService, UserManager<ApplicationUser> userManager)
        {
            _categoryService = categoryService;
            _userManager = userManager;
        }

        // GET: /Category
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            var userId = _userManager.GetUserId(User);
            var pagedCategories = await _categoryService.GetPagedCategoriesAsync(page, pageSize, userId!);

            return View(pagedCategories);
        }

        // GET: /Category/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);
            await _categoryService.CreateCategoryAsync(model.Name, userId!, model.Description);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Category/Edit/{id}
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var category = await _categoryService.GetByIdAsync(id, userId!);

            if (category == null) return NotFound();

            var model = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return View(model);
        }

        // POST: /Category/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userId = _userManager.GetUserId(User);
            await _categoryService.UpdateCategoryAsync(model.Id, userId!, model.Name, model.Description);

            return RedirectToAction(nameof(Index));
        }

        // GET: /Category/Delete/{id}
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            var category = await _categoryService.GetByIdAsync(id, userId!);

            if (category == null) return NotFound();

            var model = new CategoryDeleteViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };

            return View(model);
        }

        // POST: /Category/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            await _categoryService.DeleteCategoryAsync(id, userId!);

            return RedirectToAction(nameof(Index));
        }
    }
}