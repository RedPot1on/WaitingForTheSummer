using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Pages.Admin.Users;

public class IndexModel(UserManager<IdentityUser> userManager) : PageModel
{
    public record UserRow(string Id, string? UserName, IReadOnlyList<string> Roles);

    public IReadOnlyList<UserRow> Users { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        StatusMessage = TempData["StatusMessage"] as string;
        var users = await userManager.Users.OrderBy(u => u.UserName).ToListAsync();
        var rows = new List<UserRow>();
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            rows.Add(new UserRow(user.Id, user.UserName, roles.ToList()));
        }

        Users = rows;
    }

    public async Task<IActionResult> OnPostDeleteAsync(string id)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
            return RedirectToPage();

        if (await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            TempData["StatusMessage"] = "Администратора удалять нельзя.";
            return RedirectToPage();
        }

        await userManager.DeleteAsync(user);
        TempData["StatusMessage"] = $"Игрок {user.UserName} удалён.";
        return RedirectToPage();
    }
}
