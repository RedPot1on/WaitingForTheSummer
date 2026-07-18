using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Quests;

public class DetailsModel(ApplicationDbContext db, IQuestAccessService questAccess, IRoundService roundService) : PageModel
{
    public Quest? Quest { get; private set; }
    public IReadOnlyList<string> RequiredTitles { get; private set; } = [];
    public bool CanStart { get; private set; }
    public string? LockReason { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        Quest = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished, cancellationToken);

        if (Quest is null)
            return NotFound();

        var requiredIds = Quest.Requirements.Select(r => r.RequiredQuestId).ToList();
        RequiredTitles = await db.Quests
            .AsNoTracking()
            .Where(q => requiredIds.Contains(q.Id))
            .OrderBy(q => q.SortOrder)
            .Select(q => q.Title)
            .ToListAsync(cancellationToken);

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        (CanStart, LockReason) = await questAccess.CanStartAsync(userId, id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error, _) = await roundService.StartAsync(userId, id, cancellationToken);
        if (!ok)
        {
            TempData["StatusMessage"] = error;
            return RedirectToPage("./Index");
        }

        return RedirectToPage("/Rounds/Current");
    }
}
