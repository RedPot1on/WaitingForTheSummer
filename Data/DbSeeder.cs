using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;

        var db = sp.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Player })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = sp.GetRequiredService<UserManager<IdentityUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        await EnsureUserAsync(
            userManager,
            config["Seed:Admin1:UserName"] ?? "admin1",
            config["Seed:Admin1:Password"] ?? "Admin123!",
            AppRoles.Admin);

        await EnsureUserAsync(
            userManager,
            config["Seed:Admin2:UserName"] ?? "admin2",
            config["Seed:Admin2:Password"] ?? "Admin123!",
            AppRoles.Admin);
    }

    private static async Task EnsureUserAsync(
        UserManager<IdentityUser> userManager,
        string userName,
        string password,
        string role)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new IdentityUser
            {
                UserName = userName,
                Email = $"{userName}@local",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Не удалось создать пользователя {userName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
