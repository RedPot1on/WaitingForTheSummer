using System.ComponentModel.DataAnnotations;

namespace WaitingForTheSummer.Models;

public class Quest
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Текст и материалы на экране активного раунда (можно HTML).</summary>
    public string BodyContent { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? IconPath { get; set; }

    public bool IsPublished { get; set; } = true;

    /// <summary>После успешного прохождения нельзя начать снова.</summary>
    public bool IsOnceOnly { get; set; }

    /// <summary>Бонусный квест — доступен только в бонусном раунде победившей команде.</summary>
    public bool IsBonus { get; set; }

    /// <summary>Баллы за успешное прохождение (при провале — 0).</summary>
    [Range(0, int.MaxValue)]
    public int Points { get; set; }

    public int SortOrder { get; set; }

    public ICollection<QuestRequirement> Requirements { get; set; } = new List<QuestRequirement>();

    public ICollection<Round> Rounds { get; set; } = new List<Round>();
}
