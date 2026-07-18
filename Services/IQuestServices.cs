using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class TeamPairScore
{
    public int PairEndRegularNumber { get; init; }
    public int MalePoints { get; init; }
    public int FemalePoints { get; init; }
    public Gender? WinnerTeam { get; init; }
    public bool IsTie => WinnerTeam is null && (MalePoints > 0 || FemalePoints > 0 || MalePoints == FemalePoints);
}

public sealed class QuestRequirementStatus
{
    public required string Title { get; init; }
    public bool IsMet { get; init; }
}

public sealed class QuestBoardItem
{
    public required Quest Quest { get; init; }
    public bool CanStart { get; init; }
    public bool IsLocked { get; init; }
    public bool IsOnceCompleted { get; init; }
    public bool IsSelectedInCurrentRound { get; init; }
    public bool HasUnmetRequirements { get; init; }
    public string? LockReason { get; init; }
    public IReadOnlyList<QuestRequirementStatus> Requirements { get; init; } = [];
}

public interface IQuestAccessService
{
    Task<IReadOnlyList<QuestBoardItem>> GetBoardAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool CanStart, string? Reason, IReadOnlyList<QuestRequirementStatus> Requirements)> CanStartAsync(
        string userId,
        int questId,
        CancellationToken cancellationToken = default);
}

public interface IRoundService
{
    Task<GameRound?> GetActiveGameRoundAsync(CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, GameRound? GameRound)> StartRegularRoundAsync(string adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, GameRound? GameRound)> StartBonusRoundAsync(string adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CloseGameRoundAsync(string adminUserId, CancellationToken cancellationToken = default);

    Task<Round?> GetPlayerTakeInActiveGameRoundAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, Round? Round)> TakeQuestAsync(string userId, int questId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> ResolveAsync(int roundId, RoundStatus outcome, string adminUserId, CancellationToken cancellationToken = default);
    Task<int> GetPlayerTotalPointsAsync(string userId, CancellationToken cancellationToken = default);

    Task<TeamPairScore?> GetPendingBonusAsync(CancellationToken cancellationToken = default);
    Task<TeamPairScore?> GetLatestClosedPairScoreAsync(CancellationToken cancellationToken = default);
}
