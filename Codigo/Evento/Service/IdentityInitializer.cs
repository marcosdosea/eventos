using Microsoft.AspNetCore.Identity;

namespace Service;

public static class IdentityInitializer
{
    public static async Task InitializeRoles(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = { "ADMINISTRADOR", "GESTOR", "COLABORADOR", "USUARIO" };

        foreach (var roleName in roles)
        {
            var roleExists = await roleManager.RoleExistsAsync(roleName);
            if (!roleExists)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
} 