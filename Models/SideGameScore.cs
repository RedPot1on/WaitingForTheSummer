using System.ComponentModel.DataAnnotations;

namespace WaitingForTheSummer.Models;

/// <summary>Баллы параллельной игры (игры 1–12), независимо от квестов.</summary>
public class SideGameScore
{
    public const int GameCount = 12;
    public const decimal MinPoints = -2m;
    public const decimal MaxPoints = 2.5m;

    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Номер игры от 1 до 12.</summary>
    [Range(1, GameCount)]
    public int GameNumber { get; set; }

    [Range(typeof(decimal), "-2", "2.5")]
    public decimal? Points { get; set; }

    /// <summary>Роль в игре; null — не выбрана.</summary>
    public SideGameRole? Role { get; set; }
}
