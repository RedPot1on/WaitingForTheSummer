using System.ComponentModel.DataAnnotations;

namespace WaitingForTheSummer.Models;

/// <summary>Выбор квеста игроком внутри раунда (не больше одного на раунд).</summary>
public class Round
{
    public int Id { get; set; }

    public int GameRoundId { get; set; }
    public GameRound GameRound { get; set; } = null!;

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public int QuestId { get; set; }
    public Quest Quest { get; set; } = null!;

    public RoundStatus Status { get; set; } = RoundStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    /// <summary>Начисленные баллы после резолва (успех = баллы квеста, провал = 0).</summary>
    public int PointsAwarded { get; set; }

    public string? ResolvedByAdminId { get; set; }
    public ApplicationUser? ResolvedByAdmin { get; set; }
}
