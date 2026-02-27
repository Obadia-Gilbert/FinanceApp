using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.ExcelAc;
using FinanceApp.Application.Interfaces;
using FinanceApp.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceApp.Web.Areas.Admin
{
   [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IUserService _userService;
        private readonly ICategoryService _categoryService;

        public AdminController(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IActionResult> Users()
        {
            var users = await _userService.GetAllUsersAsync();
            return View(users);
        }

        public async Task<IActionResult> Delete(string id)
        {
            await _userService.DeleteUserAsync(id);
            return RedirectToAction("Users");
        }
        public async Task<IActionResult> AddToRole(string id, string role)
        {
            await _userService.AddUserToRoleAsync(id, role);
            return RedirectToAction("Users");
        }
        public async Task<IActionResult> Categories(string userId, bool isAdmin)
        {
            var categories = await _categoryService.GetCategoriesAsync(userId, isAdmin);
            return View(categories);
        }

  }
}