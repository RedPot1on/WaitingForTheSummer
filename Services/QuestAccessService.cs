using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class QuestAccessService(ApplicationDbContext db) : IQuestAccessService
{
    public async Task<IReadOnlyList<QuestBoardItem>> GetBoardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var quests = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .Where(q => q.IsPublished)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

        var succeededQuestIds = await db.Rounds
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == RoundStatus.Succeeded)
            .Select(r => r.QuestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var hasActiveRound = await db.Rounds
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId && r.Status == RoundStatus.InProgress, cancellationToken);

        var succeeded = succeededQuestIds.ToHashSet();
        var items = new List<QuestBoardItem>(quests.Count);

        foreach (var quest in quests)
        {
            var onceCompleted = quest.IsOnceOnly && succeeded.Contains(quest.Id);
            var missing = quest.Requirements
                .Where(r => !succeeded.Contains(r.RequiredQuestId))
                .Select(r => r.RequiredQuestId)
                .ToList();

            string? reason = null;
            if (onceCompleted)
                reason = "Квест можно пройти только один раз";
            else if (hasActiveRound)
                reason = "Сначала завершите текущий раунд";
            else if (missing.Count > 0)
                reason = "Не выполнены требования";

            var canStart = reason is null;
            items.Add(new QuestBoardItem
            {
                Quest = quest,
                CanStart = canStart,
                IsLocked = !canStart,
                IsOnceCompleted = onceCompleted,
                LockReason = reason
            });
        }

        return items;
    }

    public async Task<(bool CanStart, string? Reason)> CanStartAsync(
        string userId,
        int questId,
        CancellationToken cancellationToken = default)
    {
        var quest = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken);

        if (quest is null || !quest.IsPublished)
            return (false, "Квест недоступен");

        if (await db.Rounds.AnyAsync(
                r => r.UserId == userId && r.Status == RoundStatus.InProgress,
                cancellationToken))
            return (false, "Сначала завершите текущий раунд");

        var succeeded = await db.Rounds
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == RoundStatus.Succeeded)
            .Select(r => r.QuestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var succeededSet = succeeded.ToHashSet();

        if (quest.IsOnceOnly && succeededSet.Contains(quest.Id))
            return (false, "Квест можно пройти только один раз");

        if (quest.Requirements.Any(r => !succeededSet.Contains(r.RequiredQuestId)))
            return (false, "Не выполнены требования");

        return (true, null);
    }
}
