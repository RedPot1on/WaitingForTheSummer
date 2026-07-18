using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class RoundService(ApplicationDbContext db, IQuestAccessService questAccess) : IRoundService
{
    public Task<Round?> GetActiveRoundAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Rounds
            .Include(r => r.Quest)
            .FirstOrDefaultAsync(
                r => r.UserId == userId && r.Status == RoundStatus.InProgress,
                cancellationToken);

    public async Task<(bool Ok, string? Error, Round? Round)> StartAsync(
        string userId,
        int questId,
        CancellationToken cancellationToken = default)
    {
        var (canStart, reason) = await questAccess.CanStartAsync(userId, questId, cancellationToken);
        if (!canStart)
            return (false, reason, null);

        var round = new Round
        {
            UserId = userId,
            QuestId = questId,
            Status = RoundStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.Rounds.Add(round);
        await db.SaveChangesAsync(cancellationToken);
        await db.Entry(round).Reference(r => r.Quest).LoadAsync(cancellationToken);

        return (true, null, round);
    }

    public async Task<(bool Ok, string? Error)> ResolveAsync(
        int roundId,
        RoundStatus outcome,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (outcome is not (RoundStatus.Succeeded or RoundStatus.Failed))
            return (false, "Некорректный исход раунда");

        var round = await db.Rounds.FirstOrDefaultAsync(r => r.Id == roundId, cancellationToken);
        if (round is null)
            return (false, "Раунд не найден");

        if (round.Status != RoundStatus.InProgress)
            return (false, "Раунд уже завершён");

        round.Status = outcome;
        round.ResolvedAt = DateTime.UtcNow;
        round.ResolvedByAdminId = adminUserId;

        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
