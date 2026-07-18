using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;

namespace WaitingForTheSummer.Pages.Admin.Quests;

public class IndexModel(ApplicationDbContext db) : PageModel
{
    public record QuestRow(
        int Id,
        string Title,
        int SortOrder,
        bool IsPublished,
        bool IsOnceOnly,
        string RequirementSummary);

    public IReadOnlyList<QuestRow> Quests { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["StatusMessage"] as string;

        var quests = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

        var titles = quests.ToDictionary(q => q.Id, q => q.Title);

        Quests = quests.Select(q => new QuestRow(
            q.Id,
            q.Title,
            q.SortOrder,
            q.IsPublished,
            q.IsOnceOnly,
            q.Requirements.Count == 0
                ? "—"
                : string.Join(", ", q.Requirements.Select(r =>
                    titles.GetValueOrDefault(r.RequiredQuestId, $"#{r.RequiredQuestId}")))
        )).ToList();
    }
}
