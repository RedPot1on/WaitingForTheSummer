using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Pages.Mafia;

public class IndexModel(ApplicationDbContext db) : PageModel
{
    public record GameItem(int GameNumber, decimal? Points, SideGameRole? Role);

    public IReadOnlyList<GameItem> Games { get; private set; } = [];
    public decimal TotalPoints { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var scores = await db.SideGameScores
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.GameNumber)
            .ToListAsync(cancellationToken);

        Games = scores
            .Where(s => s.GameNumber is >= 1 and <= SideGameScore.GameCount)
            .Where(s => s.Points is not null || s.Role is not null)
            .Select(s => new GameItem(s.GameNumber, s.Points, s.Role))
            .ToList();

        TotalPoints = Games.Where(g => g.Points is not null).Sum(g => g.Points!.Value);
    }

    public string FormatPoints(decimal? points) =>
        points?.ToString("0.#", CultureInfo.InvariantCulture) ?? "—";
}
