using Microsoft.AspNetCore.Identity;

namespace Hero3.Seeds
{
    public class DefaultRoles 
    {
        public static async Task SeedAsync(RoleManager<IdentityRole> roleManager)
        {
            if(!roleManager.Roles.Any())
            {

                await roleManager.CreateAsync(new IdentityRole(AppRoles.HR));
                await roleManager.CreateAsync(new IdentityRole(AppRoles.Manager));
                await roleManager.CreateAsync(new IdentityRole(AppRoles.Employee));

            }
        }
    }
}
