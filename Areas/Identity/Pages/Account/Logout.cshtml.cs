using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WaitingForTheSummer.Areas.Identity.Pages.Account;

public class LogoutModel(SignInManager<IdentityUser> signInManager, ILogger<LogoutModel> logger) : PageModel
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
