using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;
using WaitingForTheSummer.Services;

namespace WaitingForTheSummer.Pages.Quests;

public class HistoryModel(ApplicationDbContext db, IRoundService roundService) : PageModel
{
    public record HistoryItem(
        int RoundId,
        int GameRoundNumber,
        GameRoundKind GameRoundKind,
        string QuestTitle,
        string QuestDescription,
        bool IsBonusQuest,
        int QuestPoints,
        int PointsAwarded,
        RoundStatus Status,
        DateTime StartedAt,
        DateTime? ResolvedAt);

    public IReadOnlyList<HistoryItem> Items { get; private set; } = [];
    public int TotalPoints { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        TotalPoints = await roundService.GetPlayerTotalPointsAsync(userId, cancellationToken);

        Items = await db.Rounds
            .AsNoTracking()
            .Include(r => r.Quest)
            .Include(r => r.GameRound)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.StartedAt)
            .Select(r => new HistoryItem(
                r.Id,
                r.GameRound.Number,
                r.GameRound.Kind,
                r.Quest.Title,
                r.Quest.Description,
                r.Quest.IsBonus,
                r.Quest.Points,
                r.PointsAwarded,
                r.Status,
                r.StartedAt,
                r.ResolvedAt))
            .ToListAsync(cancellationToken);
    }

    public string StatusLabel(RoundStatus status) => status switch
    {
        RoundStatus.InProgress => "Ожидает решения",
        RoundStatus.Succeeded => "Успех",
        RoundStatus.Failed => "Провал",
        _ => status.ToString()
    };

    public string StatusBadgeClass(RoundStatus status) => status switch
    {
        RoundStatus.InProgress => "text-bg-primary",
        RoundStatus.Succeeded => "text-bg-success",
        RoundStatus.Failed => "text-bg-danger",
        _ => "text-bg-secondary"
    };
}
