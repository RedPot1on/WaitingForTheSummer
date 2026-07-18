using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Services;

public sealed class QuestBoardItem
{
    public required Quest Quest { get; init; }
    public bool CanStart { get; init; }
    public bool IsLocked { get; init; }
    public bool IsOnceCompleted { get; init; }
    public string? LockReason { get; init; }
}

public interface IQuestAccessService
{
    Task<IReadOnlyList<QuestBoardItem>> GetBoardAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool CanStart, string? Reason)> CanStartAsync(string userId, int questId, CancellationToken cancellationToken = default);
}

public interface IRoundService
{
    Task<Round?> GetActiveRoundAsync(string userId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, Round? Round)> StartAsync(string userId, int questId, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> ResolveAsync(int roundId, RoundStatus outcome, string adminUserId, CancellationToken cancellationToken = default);
}
