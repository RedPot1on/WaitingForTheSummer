using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace WaitingForTheSummer.Models;

public class Round
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public IdentityUser User { get; set; } = null!;

    public int QuestId { get; set; }
    public Quest Quest { get; set; } = null!;

    public RoundStatus Status { get; set; } = RoundStatus.InProgress;

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ResolvedAt { get; set; }

    public string? ResolvedByAdminId { get; set; }
    public IdentityUser? ResolvedByAdmin { get; set; }
}
