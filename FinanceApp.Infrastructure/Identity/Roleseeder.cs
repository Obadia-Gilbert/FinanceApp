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

        // Ensure obadia@midata-tech.com is Admin (create if missing, or add to role if existing)
        string adminEmail = "obadia@midata-tech.com";
        string adminPassword = "90Barclaysnew!";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(adminUser, "Admin");
        }
        else if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}