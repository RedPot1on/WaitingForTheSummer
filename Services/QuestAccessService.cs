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

        var activeGameRound = await db.GameRounds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

        Round? takeInCurrent = null;
        if (activeGameRound is not null)
        {
            takeInCurrent = await db.Rounds
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.GameRoundId == activeGameRound.Id && r.UserId == userId,
                    cancellationToken);
        }

        var succeeded = succeededQuestIds.ToHashSet();
        var items = new List<QuestBoardItem>(quests.Count);

        foreach (var quest in quests)
        {
            var onceCompleted = quest.IsOnceOnly && succeeded.Contains(quest.Id);
            var missing = quest.Requirements
                .Where(r => !succeeded.Contains(r.RequiredQuestId))
                .Select(r => r.RequiredQuestId)
                .ToList();
            var isSelected = takeInCurrent?.QuestId == quest.Id;

            string? reason = null;
            if (activeGameRound is null)
                reason = "Раунд ещё не начат";
            else if (takeInCurrent is not null && !isSelected)
                reason = "В этом раунде вы уже взяли другой квест";
            else if (takeInCurrent is not null && isSelected)
                reason = "Квест уже выбран в этом раунде";
            else if (onceCompleted)
                reason = "Квест можно пройти только один раз";
            else if (missing.Count > 0)
                reason = "Не выполнены требования";

            var canStart = reason is null;
            items.Add(new QuestBoardItem
            {
                Quest = quest,
                CanStart = canStart,
                IsLocked = !canStart,
                IsOnceCompleted = onceCompleted,
                IsSelectedInCurrentRound = isSelected,
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

        var activeGameRound = await db.GameRounds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

        if (activeGameRound is null)
            return (false, "Раунд ещё не начат");

        var alreadyTook = await db.Rounds.AnyAsync(
            r => r.GameRoundId == activeGameRound.Id && r.UserId == userId,
            cancellationToken);

        if (alreadyTook)
            return (false, "В этом раунде вы уже взяли квест");

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
