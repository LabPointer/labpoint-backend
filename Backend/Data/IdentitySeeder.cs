using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Models;

namespace Backend.Data;

public static class IdentitySeeder
{
    /// <summary>
    /// Cria no banco uma role para cada valor do enum EAccountRole (User, Admin, Owner), se ainda não existir.
    /// </summary>
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Enum.GetNames<EAccountRole>())
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Falha ao criar a role '{role}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
        }
    }
}