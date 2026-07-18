using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class QuestAccessService(ApplicationDbContext db) : IQuestAccessService
{
    public async Task<IReadOnlyList<QuestBoardItem>> GetBoardAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return [];

        var activeGameRound = await db.GameRounds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

        var wantBonus = activeGameRound?.Kind == GameRoundKind.Bonus;

        var quests = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .Where(q => q.IsPublished && q.IsBonus == wantBonus)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .ToListAsync(cancellationToken);

        // Если раунда нет — показываем обычные квесты (заблокированные)
        if (activeGameRound is null)
        {
            quests = await db.Quests
                .AsNoTracking()
                .Include(q => q.Requirements)
                .Where(q => q.IsPublished && !q.IsBonus)
                .OrderBy(q => q.SortOrder)
                .ThenBy(q => q.Id)
                .ToListAsync(cancellationToken);
        }

        var titles = await db.Quests
            .AsNoTracking()
            .Select(q => new { q.Id, q.Title })
            .ToDictionaryAsync(q => q.Id, q => q.Title, cancellationToken);

        var succeededQuestIds = await db.Rounds
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == RoundStatus.Succeeded)
            .Select(r => r.QuestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        Round? takeInCurrent = null;
        if (activeGameRound is not null)
        {
            takeInCurrent = await db.Rounds
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    r => r.GameRoundId == activeGameRound.Id && r.UserId == userId,
                    cancellationToken);
        }

        var teamAllowed = activeGameRound is null
            || activeGameRound.Kind != GameRoundKind.Bonus
            || activeGameRound.EligibleTeam == user.Gender;

        var succeeded = succeededQuestIds.ToHashSet();
        var items = new List<QuestBoardItem>(quests.Count);

        foreach (var quest in quests)
        {
            var requirements = quest.Requirements
                .OrderBy(r => r.RequiredQuestId)
                .Select(r => new QuestRequirementStatus
                {
                    Title = titles.GetValueOrDefault(r.RequiredQuestId, $"Квест #{r.RequiredQuestId}"),
                    IsMet = succeeded.Contains(r.RequiredQuestId)
                })
                .ToList();

            var onceCompleted = quest.IsOnceOnly && succeeded.Contains(quest.Id);
            var hasUnmet = requirements.Any(r => !r.IsMet);
            var isSelected = takeInCurrent?.QuestId == quest.Id;

            string? reason = null;
            if (activeGameRound is null)
                reason = "Раунд ещё не начат";
            else if (!teamAllowed)
                reason = "Бонусный раунд — ваша команда не участвует";
            else if (takeInCurrent is not null && !isSelected)
                reason = "В этом раунде вы уже взяли другой квест";
            else if (takeInCurrent is not null && isSelected)
                reason = "Квест уже выбран в этом раунде";
            else if (onceCompleted)
                reason = "Квест можно пройти только один раз";
            else if (hasUnmet)
                reason = null;

            var canStart = activeGameRound is not null
                && teamAllowed
                && takeInCurrent is null
                && !onceCompleted
                && !hasUnmet;

            items.Add(new QuestBoardItem
            {
                Quest = quest,
                CanStart = canStart,
                IsLocked = !canStart,
                IsOnceCompleted = onceCompleted,
                IsSelectedInCurrentRound = isSelected,
                HasUnmetRequirements = hasUnmet,
                LockReason = reason,
                Requirements = requirements
            });
        }

        return items;
    }

    public async Task<(bool CanStart, string? Reason, IReadOnlyList<QuestRequirementStatus> Requirements)> CanStartAsync(
        string userId,
        int questId,
        CancellationToken cancellationToken = default)
    {
        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
            return (false, "Пользователь не найден", []);

        var quest = await db.Quests
            .AsNoTracking()
            .Include(q => q.Requirements)
            .FirstOrDefaultAsync(q => q.Id == questId, cancellationToken);

        if (quest is null || !quest.IsPublished)
            return (false, "Квест недоступен", []);

        var titles = await db.Quests
            .AsNoTracking()
            .Where(q => quest.Requirements.Select(r => r.RequiredQuestId).Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.Title, cancellationToken);

        var succeeded = await db.Rounds
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == RoundStatus.Succeeded)
            .Select(r => r.QuestId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var succeededSet = succeeded.ToHashSet();

        var requirements = quest.Requirements
            .OrderBy(r => r.RequiredQuestId)
            .Select(r => new QuestRequirementStatus
            {
                Title = titles.GetValueOrDefault(r.RequiredQuestId, $"Квест #{r.RequiredQuestId}"),
                IsMet = succeededSet.Contains(r.RequiredQuestId)
            })
            .ToList();

        var activeGameRound = await db.GameRounds
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

        if (activeGameRound is null)
            return (false, "Раунд ещё не начат", requirements);

        if (activeGameRound.Kind == GameRoundKind.Bonus)
        {
            if (!quest.IsBonus)
                return (false, "В бонусном раунде доступны только бонусные квесты", requirements);
            if (activeGameRound.EligibleTeam != user.Gender)
                return (false, "Бонусный раунд — ваша команда не участвует", requirements);
        }
        else if (quest.IsBonus)
        {
            return (false, "Бонусный квест доступен только в бонусном раунде", requirements);
        }

        var alreadyTook = await db.Rounds.AnyAsync(
            r => r.GameRoundId == activeGameRound.Id && r.UserId == userId,
            cancellationToken);

        if (alreadyTook)
            return (false, "В этом раунде вы уже взяли квест", requirements);

        if (quest.IsOnceOnly && succeededSet.Contains(quest.Id))
            return (false, "Квест можно пройти только один раз", requirements);

        if (requirements.Any(r => !r.IsMet))
            return (false, null, requirements);

        return (true, null, requirements);
    }
}
