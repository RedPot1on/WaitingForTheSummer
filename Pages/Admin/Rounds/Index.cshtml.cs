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
    public record TakeRow(
        int Id,
        string? UserName,
        string QuestTitle,
        int QuestPoints,
        int PointsAwarded,
        RoundStatus Status,
        DateTime StartedAt);

    public GameRound? ActiveGameRound { get; private set; }
    public IReadOnlyList<TakeRow> ActiveTakes { get; private set; } = [];
    public IReadOnlyList<GameRound> PastGameRounds { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["StatusMessage"] as string;
        ActiveGameRound = await roundService.GetActiveGameRoundAsync(cancellationToken);

        if (ActiveGameRound is not null)
        {
            ActiveTakes = await db.Rounds
                .AsNoTracking()
                .Include(r => r.User)
                .Include(r => r.Quest)
                .Where(r => r.GameRoundId == ActiveGameRound.Id)
                .OrderBy(r => r.StartedAt)
                .Select(r => new TakeRow(
                    r.Id,
                    r.User.UserName,
                    r.Quest.Title,
                    r.Quest.Points,
                    r.PointsAwarded,
                    r.Status,
                    r.StartedAt))
                .ToListAsync(cancellationToken);
        }

        PastGameRounds = await db.GameRounds
            .AsNoTracking()
            .OrderByDescending(g => g.Number)
            .Take(20)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostStartAsync(CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error, gameRound) = await roundService.StartGameRoundAsync(adminId, cancellationToken);
        TempData["StatusMessage"] = ok
            ? $"Раунд № {gameRound!.Number} начат."
            : error;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCloseAsync(CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await roundService.CloseGameRoundAsync(adminId, cancellationToken);
        TempData["StatusMessage"] = ok ? "Раунд закрыт." : error;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResolveAsync(
        int roundId,
        RoundStatus outcome,
        CancellationToken cancellationToken)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var (ok, error) = await roundService.ResolveAsync(roundId, outcome, adminId, cancellationToken);
        TempData["StatusMessage"] = ok ? "Исход сохранён." : error;
        return RedirectToPage();
    }

    public string StatusLabel(RoundStatus status) => status switch
    {
        RoundStatus.InProgress => "Ожидает",
        RoundStatus.Succeeded => "Успех",
        RoundStatus.Failed => "Провал",
        _ => status.ToString()
    };
}
