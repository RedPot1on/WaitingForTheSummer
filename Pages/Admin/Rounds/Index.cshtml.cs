using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Admin.Rounds;

public class IndexModel(ApplicationDbContext db, IRoundService roundService) : PageModel
{
    public record RoundRow(
        int Id,
        string? UserName,
        string QuestTitle,
        RoundStatus Status,
        DateTime StartedAt,
        DateTime? ResolvedAt);

    public IReadOnlyList<RoundRow> Active { get; private set; } = [];
    public IReadOnlyList<RoundRow> History { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["StatusMessage"] as string;
        var rows = await LoadRowsAsync(cancellationToken);
        Active = rows.Where(r => r.Status == RoundStatus.InProgress).ToList();
        History = rows.Where(r => r.Status != RoundStatus.InProgress).Take(50).ToList();
    }

    public async Task<IActionResult> OnPostResolveAsync(
        int roundId,
        RoundStatus outcome,
        CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await roundService.ResolveAsync(roundId, outcome, adminId, cancellationToken);
        TempData["StatusMessage"] = ok ? "Раунд обновлён." : error;
        return RedirectToPage();
    }

    private async Task<List<RoundRow>> LoadRowsAsync(CancellationToken cancellationToken)
    {
        return await db.Rounds
            .AsNoTracking()
            .Include(r => r.User)
            .Include(r => r.Quest)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new RoundRow(
                r.Id,
                r.User.UserName,
                r.Quest.Title,
                r.Status,
                r.StartedAt,
                r.ResolvedAt))
            .ToListAsync(cancellationToken);
    }
}
