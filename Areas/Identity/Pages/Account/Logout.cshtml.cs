using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Areas.Identity.Pages.Account;

public class LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger) : PageModel
{
    public async Task<IActionResult> OnPost(string? returnUrl = null)
    {
        await signInManager.SignOutAsync();
        logger.LogInformation("User logged out.");

        if (returnUrl is not null)
            return LocalRedirect(returnUrl);

        return RedirectToPage();
    }

    public void OnGet()
    {
    }
}
