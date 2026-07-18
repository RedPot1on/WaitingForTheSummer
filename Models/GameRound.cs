using System.ComponentModel.DataAnnotations;

namespace WaitingForTheSummer.Models;

/// <summary>Раунд, который стартует и закрывает администратор.</summary>
public class GameRound
{
    public int Id { get; set; }

    public int Number { get; set; }

    public GameRoundKind Kind { get; set; } = GameRoundKind.Regular;

    /// <summary>Для бонусного раунда — какая команда участвует.</summary>
    public Gender? EligibleTeam { get; set; }

    /// <summary>Номер последней обычной пары раундов (2, 4, 6…), за которую выдан бонус.</summary>
    public int? BonusForRegularPairEnd { get; set; }

    public GameRoundStatus Status { get; set; } = GameRoundStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string StartedByAdminId { get; set; } = string.Empty;
    public ApplicationUser StartedByAdmin { get; set; } = null!;

    public DateTime? ClosedAt { get; set; }

    public string? ClosedByAdminId { get; set; }
    public ApplicationUser? ClosedByAdmin { get; set; }

    public ICollection<Round> QuestTakes { get; set; } = new List<Round>();
}
