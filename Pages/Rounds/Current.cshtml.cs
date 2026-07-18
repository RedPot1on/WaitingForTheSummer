using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaitingForTheSummer.Models;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Rounds;

public class CurrentModel(IRoundService roundService) : PageModel
{
    public Round? Round { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Round = await roundService.GetActiveRoundAsync(userId, cancellationToken);
    }
}
