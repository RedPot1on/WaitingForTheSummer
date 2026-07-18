using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WaitingForTheSummer.Areas.Identity.Pages.Account;

public class ForgotPasswordModel : PageModel
{
    public IActionResult OnGet() => NotFound();

    public IActionResult OnPost() => NotFound();
}
