using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Pages.Admin.Users;

public class CreateModel(UserManager<ApplicationUser> userManager) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, Display(Name = "Логин")]
        public string UserName { get; set; } = string.Empty;

        [Required, Display(Name = "Пароль"), DataType(DataType.Password), MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required, Display(Name = "Команда")]
        public Gender Gender { get; set; } = Gender.Male;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var user = new ApplicationUser
        {
            UserName = Input.UserName,
            Email = $"u{Guid.NewGuid():N}@local",
            EmailConfirmed = true,
            Gender = Input.Gender
        };

        var result = await userManager.CreateAsync(user, Input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        await userManager.AddToRoleAsync(user, AppRoles.Player);
        TempData["StatusMessage"] = $"Игрок {Input.UserName} создан ({TeamNames.For(Input.Gender)}).";
        return RedirectToPage("./Index");
    }
}
