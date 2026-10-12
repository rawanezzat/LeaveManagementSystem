using Microsoft.AspNetCore.Identity;
using Hero3.Core.Models;

namespace Hero3.Seeds
{
    public static class DefaultUsers
    {
        public static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            var existingAdmin = await userManager.FindByNameAsync("rawan");
            if (existingAdmin == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = "rawan",
                    Email = "rawanezzat664@gmail.com",
                    EmailConfirmed = true,
                    UserPosition = "HR Manager",
                    UserDepartment = "HR",
                    HireDate = DateTime.Now
                };

                var result = await userManager.CreateAsync(admin, "Aa123@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, AppRoles.HR);
                }

            }
        }
    }
}
