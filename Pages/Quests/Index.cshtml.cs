using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Quests;

public class IndexModel(IQuestAccessService questAccess, IRoundService roundService) : PageModel
{
    public IReadOnlyList<QuestBoardItem> Items { get; private set; } = [];
    public bool HasActiveRound { get; private set; }
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        Items = await questAccess.GetBoardAsync(userId, cancellationToken);
        HasActiveRound = await roundService.GetActiveRoundAsync(userId, cancellationToken) is not null;
        StatusMessage = TempData["StatusMessage"] as string;
    }

    public async Task<IActionResult> OnPostStartAsync(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error, _) = await roundService.StartAsync(userId, id, cancellationToken);

        if (!ok)
        {
            TempData["StatusMessage"] = error;
            return RedirectToPage();
        }

        return RedirectToPage("/Rounds/Current");
    }
}
