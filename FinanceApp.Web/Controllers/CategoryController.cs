using FinanceApp.Application.Interfaces.Services;
using FinanceApp.Application.Common;    
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
      public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 10)
{
    var userId = _userManager.GetUserId(User); // get logged-in user
    var pagedCategories = await _categoryService.GetPagedCategoriesAsync(
        pageNumber, pageSize, userId
    );

    // Map Category -> CategoryViewModel
    var pagedViewModel = new PagedResult<CategoryViewModel>
    {
        Items = pagedCategories.Items.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Icon = c.Icon,
            BadgeColor = c.BadgeColor
        }).ToList(),
        PageNumber = pagedCategories.PageNumber,
        PageSize = pagedCategories.PageSize,
        TotalItems = pagedCategories.TotalItems
    };

    return View(pagedViewModel);
}
    // GET: /Category/Create (optional ?partial=true or AJAX for offcanvas)
    public IActionResult Create(bool partial = false)
    {
        bool isAjax = partial ||
                     string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        if (isAjax)
            return PartialView("_CategoryCreatePartial", new CategoryCreateViewModel());

        return View(new CategoryCreateViewModel());
    }

        // POST: /Category/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateViewModel model)
        {
            var userId = _userManager.GetUserId(User);
            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (!ModelState.IsValid)
            {
                if (isAjax)
                    return PartialView("_CategoryCreatePartial", model);
                return View(model);
            }

            await _categoryService.CreateCategoryAsync(
                model.Name,
                userId!,
                model.Description,
                model.Icon,
                model.BadgeColor
            );

            if (isAjax)
                return Json(new { success = true });

            return RedirectToAction(nameof(Index));
        }

        // GET: /Category/Edit/{id}
        // Optional ?partial=true or AJAX header to return layout-less form for offcanvas
        public async Task<IActionResult> Edit(Guid id, bool partial = false)
        {
            var userId = _userManager.GetUserId(User);
            var category = await _categoryService.GetByIdAsync(id, userId!);

            if (category == null) return NotFound();

            var model = new CategoryEditViewModel
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                Icon = category.Icon,
                BadgeColor = category.BadgeColor
            };

            bool isAjax = partial ||
                         string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (isAjax)
            {
                return PartialView("_CategoryEditPartial", model);
            }

            return View(model);
        }

        // POST: /Category/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryEditViewModel model)
        {
            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (!ModelState.IsValid)
            {
                if (isAjax)
                    return PartialView("_CategoryEditPartial", model);
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            await _categoryService.UpdateCategoryAsync(
                model.Id,
                userId!,
                model.Name,
                model.Description,
                model.Icon,
                model.BadgeColor
            );

            if (isAjax)
                return Json(new { success = true });

            return RedirectToAction(nameof(Index));
        }

        // GET: /Category/Delete/{id}
        public async Task<IActionResult> Delete(Guid id, bool partial = false)
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

            bool isAjax = partial || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (isAjax)
            {
                return PartialView("_CategoryDeletePartial", model);
            }

            return View(model);
        }
        

        // POST: /Category/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var userId = _userManager.GetUserId(User);
            await _categoryService.DeleteCategoryAsync(id, userId!);

            bool isAjax = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
            if (isAjax)
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}