using Microsoft.AspNetCore.Identity;

namespace FinanceApp.Infrastructure.Identity;

public static class RoleSeeder
{
    public static async Task SeedRolesAndAdminAsync(UserManager<ApplicationUser> userManager,
                                                    RoleManager<IdentityRole> roleManager)
    {
        // Define roles
        string[] roles = new[] { "Admin", "User" };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed admin user
        string adminEmail = "admin@financeapp.com";
        string adminPassword = "Admin123!"; // choose a strong password

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }
}