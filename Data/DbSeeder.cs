using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Data;

public static class DbSeeder
{
    private static readonly (string Name, Gender Gender)[] Players =
    [
        ("Иван", Gender.Male),
        ("Алексей", Gender.Male),
        ("Дмитрий", Gender.Male),
        ("Сергей", Gender.Male),
        ("Андрей", Gender.Male),
        ("Анна", Gender.Female),
        ("Мария", Gender.Female),
        ("Елена", Gender.Female),
        ("Ольга", Gender.Female),
        ("Наталья", Gender.Female)
    ];

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

        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var config = sp.GetRequiredService<IConfiguration>();

        await EnsureUserAsync(
            userManager,
            config["Seed:Admin1:UserName"] ?? "admin1",
            config["Seed:Admin1:Password"] ?? "Admin123!",
            AppRoles.Admin,
            Gender.Male);

        await EnsureUserAsync(
            userManager,
            config["Seed:Admin2:UserName"] ?? "admin2",
            config["Seed:Admin2:Password"] ?? "Admin123!",
            AppRoles.Admin,
            Gender.Female);

        foreach (var (name, gender) in Players)
        {
            await EnsureUserAsync(userManager, name, "123456", AppRoles.Player, gender);
        }
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string userName,
        string password,
        string role,
        Gender gender)
    {
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = userName,
                Email = $"u{Guid.NewGuid():N}@local",
                EmailConfirmed = true,
                Gender = gender
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(
                    $"Не удалось создать пользователя {userName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
        else if (user.Gender != gender && role == AppRoles.Player)
        {
            user.Gender = gender;
            await userManager.UpdateAsync(user);
        }

        if (!await userManager.IsInRoleAsync(user, role))
            await userManager.AddToRoleAsync(user, role);
    }
}
