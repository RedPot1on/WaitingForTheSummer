using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Pages.Admin.SideGames;

public class IndexModel(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : PageModel
{
    public record CellData(decimal? Points, SideGameRole? Role);
    public record PlayerRow(string UserId, string UserName, CellData[] Cells, decimal Total);

    public IReadOnlyList<PlayerRow> Players { get; private set; } = [];
    public string? StatusMessage { get; private set; }
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        StatusMessage = TempData["StatusMessage"] as string;
        ErrorMessage = TempData["ErrorMessage"] as string;
        Players = await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var players = await GetPlayersAsync();
        var playerIds = players.Select(p => p.Id).ToList();
        var existing = await db.SideGameScores
            .Where(s => playerIds.Contains(s.UserId))
            .ToListAsync(cancellationToken);

        var byKey = existing.ToDictionary(s => (s.UserId, s.GameNumber));
        var errors = new List<string>();

        foreach (var userId in playerIds)
        {
            for (var game = 1; game <= SideGameScore.GameCount; game++)
            {
                var scoreKey = $"score_{userId}_{game}";
                var roleKey = $"role_{userId}_{game}";
                var rawScore = Request.Form[scoreKey].ToString()?.Trim();
                var rawRole = Request.Form[roleKey].ToString()?.Trim();

                var hasScore = !string.IsNullOrWhiteSpace(rawScore);
                SideGameRole? role = null;
                if (!string.IsNullOrWhiteSpace(rawRole)
                    && Enum.TryParse<SideGameRole>(rawRole, out var parsedRole)
                    && Enum.IsDefined(parsedRole))
                {
                    role = parsedRole;
                }

                if (!hasScore && role is null)
                {
                    if (byKey.Remove((userId, game), out var toDelete))
                        db.SideGameScores.Remove(toDelete);
                    continue;
                }

                decimal? points = null;
                if (hasScore)
                {
                    rawScore = rawScore!.Replace(',', '.');
                    if (!decimal.TryParse(rawScore, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                    {
                        errors.Add($"Некорректное значение «{rawScore}».");
                        continue;
                    }

                    if (parsed < SideGameScore.MinPoints || parsed > SideGameScore.MaxPoints)
                    {
                        errors.Add($"Баллы должны быть от {SideGameScore.MinPoints} до {SideGameScore.MaxPoints}.");
                        continue;
                    }

                    points = parsed;
                }

                if (byKey.TryGetValue((userId, game), out var row))
                {
                    row.Points = points;
                    row.Role = role;
                }
                else
                {
                    db.SideGameScores.Add(new SideGameScore
                    {
                        UserId = userId,
                        GameNumber = game,
                        Points = points,
                        Role = role
                    });
                }
            }
        }

        if (errors.Count > 0)
        {
            TempData["ErrorMessage"] = string.Join(" ", errors.Distinct().Take(5));
            return RedirectToPage();
        }

        await db.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Таблица сохранена.";
        return RedirectToPage();
    }

    private async Task<List<ApplicationUser>> GetPlayersAsync()
    {
        var inPlayerRole = await userManager.GetUsersInRoleAsync(AppRoles.Player);
        var admins = await userManager.GetUsersInRoleAsync(AppRoles.Admin);
        var adminIds = admins.Select(a => a.Id).ToHashSet();

        return inPlayerRole
            .Where(u => !adminIds.Contains(u.Id))
            .OrderBy(u => u.UserName)
            .ToList();
    }

    private async Task<IReadOnlyList<PlayerRow>> LoadAsync(CancellationToken cancellationToken)
    {
        var players = await GetPlayersAsync();
        var ids = players.Select(p => p.Id).ToList();
        var scores = await db.SideGameScores
            .AsNoTracking()
            .Where(s => ids.Contains(s.UserId))
            .ToListAsync(cancellationToken);

        var lookup = scores.ToLookup(s => s.UserId);

        return players.Select(p =>
        {
            var cells = new CellData[SideGameScore.GameCount];
            for (var i = 0; i < cells.Length; i++)
                cells[i] = new CellData(null, null);

            decimal total = 0;
            foreach (var s in lookup[p.Id])
            {
                if (s.GameNumber is >= 1 and <= SideGameScore.GameCount)
                {
                    cells[s.GameNumber - 1] = new CellData(s.Points, s.Role);
                    if (s.Points is { } pts)
                        total += pts;
                }
            }

            return new PlayerRow(p.Id, p.UserName ?? p.Id, cells, total);
        }).ToList();
    }
}
