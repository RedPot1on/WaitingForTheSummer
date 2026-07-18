using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaitingForTheSummer.Models;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Rounds;

public class CurrentModel(IRoundService roundService) : PageModel
{
    public GameRound? ActiveGameRound { get; private set; }
    public Round? Round { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        ActiveGameRound = await roundService.GetActiveGameRoundAsync(cancellationToken);
        Round = await roundService.GetPlayerTakeInActiveGameRoundAsync(userId, cancellationToken);
    }
}
