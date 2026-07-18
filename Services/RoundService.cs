using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class RoundService(ApplicationDbContext db, IQuestAccessService questAccess) : IRoundService
{
    public Task<GameRound?> GetActiveGameRoundAsync(CancellationToken cancellationToken = default) =>
        db.GameRounds.FirstOrDefaultAsync(g => g.Status == GameRoundStatus.Active, cancellationToken);

    public async Task<(bool Ok, string? Error, GameRound? GameRound)> StartRegularRoundAsync(
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var authError = await ValidateAdminAsync(adminUserId, cancellationToken);
        if (authError is not null)
            return (false, authError, null);

        if (await db.GameRounds.AnyAsync(g => g.Status == GameRoundStatus.Active, cancellationToken))
            return (false, "Уже есть активный раунд. Сначала закройте его.", null);

        var pending = await GetPendingBonusAsync(cancellationToken);
        if (pending?.WinnerTeam is not null)
            return (false, $"Сначала проведите бонусный раунд для команды «{TeamNames.For(pending.WinnerTeam.Value)}».", null);

        return await CreateRoundAsync(
            adminUserId,
            GameRoundKind.Regular,
            eligibleTeam: null,
            bonusForPairEnd: null,
            cancellationToken);
    }

    public async Task<(bool Ok, string? Error, GameRound? GameRound)> StartBonusRoundAsync(
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var authError = await ValidateAdminAsync(adminUserId, cancellationToken);
        if (authError is not null)
            return (false, authError, null);

        if (await db.GameRounds.AnyAsync(g => g.Status == GameRoundStatus.Active, cancellationToken))
            return (false, "Уже есть активный раунд. Сначала закройте его.", null);

        var pending = await GetPendingBonusAsync(cancellationToken);
        if (pending is null)
            return (false, "Бонусный раунд пока не доступен. Нужна завершённая пара обычных раундов.", null);

        if (pending.WinnerTeam is null)
            return (false, "Ничья в последней паре раундов — бонусный раунд не проводится.", null);

        return await CreateRoundAsync(
            adminUserId,
            GameRoundKind.Bonus,
            eligibleTeam: pending.WinnerTeam,
            bonusForPairEnd: pending.PairEndRegularNumber,
            cancellationToken);
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
        var (canStart, reason, _) = await questAccess.CanStartAsync(userId, questId, cancellationToken);
        if (!canStart)
            return (false, reason ?? "Квест недоступен", null);

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

        var round = await db.Rounds
            .Include(r => r.Quest)
            .FirstOrDefaultAsync(r => r.Id == roundId, cancellationToken);
        if (round is null)
            return (false, "Запись не найдена");

        if (round.Status != RoundStatus.InProgress)
            return (false, "Исход уже выставлен");

        round.Status = outcome;
        round.ResolvedAt = DateTime.UtcNow;
        round.ResolvedByAdminId = adminUserId;
        round.PointsAwarded = outcome == RoundStatus.Succeeded ? round.Quest.Points : 0;

        await db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public Task<int> GetPlayerTotalPointsAsync(string userId, CancellationToken cancellationToken = default) =>
        db.Rounds
            .AsNoTracking()
            .Where(r => r.UserId == userId && r.Status == RoundStatus.Succeeded)
            .SumAsync(r => (int?)r.PointsAwarded ?? 0, cancellationToken);

    public async Task<TeamPairScore?> GetPendingBonusAsync(CancellationToken cancellationToken = default)
    {
        var score = await GetLatestClosedPairScoreAsync(cancellationToken);
        if (score is null)
            return null;

        var bonusExists = await db.GameRounds.AnyAsync(
            g => g.Kind == GameRoundKind.Bonus && g.BonusForRegularPairEnd == score.PairEndRegularNumber,
            cancellationToken);

        return bonusExists ? null : score;
    }

    public async Task<TeamPairScore?> GetLatestClosedPairScoreAsync(CancellationToken cancellationToken = default)
    {
        var closedRegular = await db.GameRounds
            .AsNoTracking()
            .Where(g => g.Kind == GameRoundKind.Regular && g.Status == GameRoundStatus.Closed)
            .OrderBy(g => g.Number)
            .Select(g => new { g.Id, g.Number })
            .ToListAsync(cancellationToken);

        if (closedRegular.Count < 2)
            return null;

        // Берём последнюю полную пару: (1,2), (3,4), …
        var pairIndex = closedRegular.Count / 2;
        var pair = closedRegular.Skip((pairIndex - 1) * 2).Take(2).ToList();
        if (pair.Count < 2)
            return null;

        var roundIds = pair.Select(p => p.Id).ToList();
        var pairEnd = pair[^1].Number;

        var points = await db.Rounds
            .AsNoTracking()
            .Where(r => roundIds.Contains(r.GameRoundId))
            .Join(db.Users, r => r.UserId, u => u.Id, (r, u) => new { u.Gender, r.PointsAwarded })
            .GroupBy(x => x.Gender)
            .Select(g => new { Gender = g.Key, Points = g.Sum(x => x.PointsAwarded) })
            .ToListAsync(cancellationToken);

        var male = points.FirstOrDefault(p => p.Gender == Gender.Male)?.Points ?? 0;
        var female = points.FirstOrDefault(p => p.Gender == Gender.Female)?.Points ?? 0;

        Gender? winner = male == female ? null : male > female ? Gender.Male : Gender.Female;

        return new TeamPairScore
        {
            PairEndRegularNumber = pairEnd,
            MalePoints = male,
            FemalePoints = female,
            WinnerTeam = winner
        };
    }

    private async Task<string?> ValidateAdminAsync(string adminUserId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(adminUserId))
            return "Сессия недействительна. Выйдите и войдите снова.";

        var adminExists = await db.Users.AnyAsync(u => u.Id == adminUserId, cancellationToken);
        return adminExists ? null : "Сессия устарела (пользователь не найден). Выйдите и войдите снова.";
    }

    private async Task<(bool Ok, string? Error, GameRound? GameRound)> CreateRoundAsync(
        string adminUserId,
        GameRoundKind kind,
        Gender? eligibleTeam,
        int? bonusForPairEnd,
        CancellationToken cancellationToken)
    {
        var nextNumber = await db.GameRounds.AnyAsync(cancellationToken)
            ? await db.GameRounds.MaxAsync(g => g.Number, cancellationToken) + 1
            : 1;

        var gameRound = new GameRound
        {
            Number = nextNumber,
            Kind = kind,
            EligibleTeam = eligibleTeam,
            BonusForRegularPairEnd = bonusForPairEnd,
            Status = GameRoundStatus.Active,
            StartedAt = DateTime.UtcNow,
            StartedByAdminId = adminUserId
        };

        db.GameRounds.Add(gameRound);
        await db.SaveChangesAsync(cancellationToken);
        return (true, null, gameRound);
    }
}
