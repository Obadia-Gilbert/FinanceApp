using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceApp.Application.DTOs;
using FinanceApp.Domain.Entities; // or the appropriate namespace where ApplicationUser is defined

namespace FinanceApp.Application.Interfaces
{
    public interface IUserService
    {
    Task<List<UserDto>> GetAllUsersAsync();
    Task DeleteUserAsync(string userId);
    Task AddUserToRoleAsync(string userId, string role);

    }
}