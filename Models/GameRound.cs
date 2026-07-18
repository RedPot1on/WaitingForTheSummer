using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WaitingForTheSummer.Models;

/// <summary>Раунд, который стартует и закрывает администратор.</summary>
public class GameRound
{
    public int Id { get; set; }

    public int Number { get; set; }

    public GameRoundStatus Status { get; set; } = GameRoundStatus.Active;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string StartedByAdminId { get; set; } = string.Empty;
    public IdentityUser StartedByAdmin { get; set; } = null!;

    public DateTime? ClosedAt { get; set; }

    public string? ClosedByAdminId { get; set; }
    public IdentityUser? ClosedByAdmin { get; set; }

    public ICollection<Round> QuestTakes { get; set; } = new List<Round>();
}
