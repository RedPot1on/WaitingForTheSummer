using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

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
    Task<(bool Ok, string? Error, GameRound? GameRound)> StartGameRoundAsync(string adminUserId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> CloseGameRoundAsync(string adminUserId, CancellationToken cancellationToken = default);

    Task<Round?> GetPlayerTakeInActiveGameRoundAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, Round? Round)> TakeQuestAsync(string userId, int questId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> ResolveAsync(int roundId, RoundStatus outcome, string adminUserId, CancellationToken cancellationToken = default);
    Task<int> GetPlayerTotalPointsAsync(string userId, CancellationToken cancellationToken = default);
}
