using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class RoundService(ApplicationDbContext db, IQuestAccessService questAccess) : IRoundService
{
    public Task<GameRound?> GetActiveGameRoundAsync(CancellationToken cancellationToken = default) =>
        db.GameRounds.FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

    public async Task<(bool Ok, string? Error, GameRound? GameRound)> StartGameRoundAsync(
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(adminUserId))
            return (false, "Сессия недействительна. Выйдите и войдите снова.", null);

        var adminExists = await db.Users.AnyAsync(u => u.Id == adminUserId, cancellationToken);
        if (!adminExists)
            return (false, "Сессия устарела (пользователь не найден). Выйдите и войдите снова.", null);

        if (await db.GameRounds.AnyAsync(g => g.Status == GameRoundStatus.Active, cancellationToken))
            return (false, "Уже есть активный раунд. Сначала закройте его.", null);

        var nextNumber = await db.GameRounds.AnyAsync(cancellationToken)
            ? await db.GameRounds.MaxAsync(g => g.Number, cancellationToken) + 1
            : 1;

        var gameRound = new GameRound
        {
            Number = nextNumber,
            Status = GameRoundStatus.Active,
            StartedAt = DateTime.UtcNow,
            StartedByAdminId = adminUserId
        };

        db.GameRounds.Add(gameRound);
        await db.SaveChangesAsync(cancellationToken);
        return (true, null, gameRound);
    }

    public async Task<(bool Ok, string? Error)> CloseGameRoundAsync(
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var active = await GetActiveGameRoundAsync(cancellationToken);
        if (active is null)
            return (false, "Нет активного раунда");

        var unresolved = await db.Rounds.CountAsync(
            r => r.GameRoundId == active.Id && r.Status == RoundStatus.InProgress,
            cancellationToken);

        if (unresolved > 0)
            return (false, $"Нельзя закрыть раунд: есть нерешённые взятия квестов ({unresolved}).");

        active.Status = GameRoundStatus.Closed;
        active.ClosedAt = DateTime.UtcNow;
        active.ClosedByAdminId = adminUserId;
        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<Round?> GetPlayerTakeInActiveGameRoundAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var active = await GetActiveGameRoundAsync(cancellationToken);
        if (active is null)
            return null;

        return await db.Rounds
            .Include(r => r.Quest)
            .Include(r => r.GameRound)
            .FirstOrDefaultAsync(
                r => r.GameRoundId == active.Id && r.UserId == userId,
                cancellationToken);
    }

    public async Task<(bool Ok, string? Error, Round? Round)> TakeQuestAsync(
        string userId,
        int questId,
        CancellationToken cancellationToken = default)
    {
        var (canStart, reason) = await questAccess.CanStartAsync(userId, questId, cancellationToken);
        if (!canStart)
            return (false, reason, null);

        var active = await GetActiveGameRoundAsync(cancellationToken);
        if (active is null)
            return (false, "Раунд ещё не начат", null);

        var round = new Round
        {
            GameRoundId = active.Id,
            UserId = userId,
            QuestId = questId,
            Status = RoundStatus.InProgress,
            StartedAt = DateTime.UtcNow
        };

        db.Rounds.Add(round);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return (false, "В этом раунде вы уже взяли квест", null);
        }

        await db.Entry(round).Reference(r => r.Quest).LoadAsync(cancellationToken);
        await db.Entry(round).Reference(r => r.GameRound).LoadAsync(cancellationToken);

        return (true, null, round);
    }

    public async Task<(bool Ok, string? Error)> ResolveAsync(
        int roundId,
        RoundStatus outcome,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        if (outcome is not (RoundStatus.Succeeded or RoundStatus.Failed))
            return (false, "Некорректный исход");

        var round = await db.Rounds.FirstOrDefaultAsync(r => r.Id == roundId, cancellationToken);
        if (round is null)
            return (false, "Запись не найдена");

        if (round.Status != RoundStatus.InProgress)
            return (false, "Исход уже выставлен");

        round.Status = outcome;
        round.ResolvedAt = DateTime.UtcNow;
        round.ResolvedByAdminId = adminUserId;

        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }
}
