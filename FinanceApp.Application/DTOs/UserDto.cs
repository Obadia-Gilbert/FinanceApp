using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FinanceApp.Application.DTOs
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? ProfileImagePath { get; set; }
        /// <summary>Comma-separated role names or single role.</summary>
        public string Role { get; set; } = string.Empty;
    }
}