using Microsoft.AspNetCore.Identity;

namespace Service;

public static class IdentityInitializer
{
    public static async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
    {
        foreach (var roleName in Core.PapelMap.AllRoles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
} 