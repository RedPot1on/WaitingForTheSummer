using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Data;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Pages.Admin.Quests;

public class EditModel(ApplicationDbContext db, IWebHostEnvironment env) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public IFormFile? IconFile { get; set; }

    [BindProperty]
    public List<int> SelectedRequirementIds { get; set; } = [];

    public IReadOnlyList<QuestOption> RequirementOptions { get; private set; } = [];

    public record QuestOption(int Id, string Title);

    public class InputModel
    {
        public int Id { get; set; }

        [Required, Display(Name = "Название"), MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required, Display(Name = "Описание"), MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Материалы раунда")]
        public string BodyContent { get; set; } = string.Empty;

        [Display(Name = "Порядок")]
        public int SortOrder { get; set; }

        [Display(Name = "Опубликован")]
        public bool IsPublished { get; set; } = true;

        [Display(Name = "Только один раз")]
        public bool IsOnceOnly { get; set; }

        [Display(Name = "Бонусный квест")]
        public bool IsBonus { get; set; }

        [Required(ErrorMessage = "Укажите баллы")]
        [Range(0, int.MaxValue, ErrorMessage = "Баллы не могут быть отрицательными")]
        [Display(Name = "Баллы")]
        public int Points { get; set; }

        public string? ExistingIconPath { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id is null)
        {
            await LoadOptionsAsync(0, cancellationToken);
            return Page();
        }

        var quest = await db.Quests
            .Include(q => q.Requirements)
            .FirstOrDefaultAsync(q => q.Id == id, cancellationToken);

        if (quest is null)
            return NotFound();

        Input = new InputModel
        {
            Id = quest.Id,
            Title = quest.Title,
            Description = quest.Description,
            BodyContent = quest.BodyContent,
            SortOrder = quest.SortOrder,
            IsPublished = quest.IsPublished,
            IsOnceOnly = quest.IsOnceOnly,
            IsBonus = quest.IsBonus,
            Points = quest.Points,
            ExistingIconPath = quest.IconPath
        };

        SelectedRequirementIds = quest.Requirements.Select(r => r.RequiredQuestId).ToList();
        await LoadOptionsAsync(quest.Id, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(Input.Id, cancellationToken);

        if (SelectedRequirementIds.Contains(Input.Id))
            ModelState.AddModelError(string.Empty, "Квест не может требовать сам себя.");

        if (!ModelState.IsValid)
            return Page();

        Quest quest;
        if (Input.Id == 0)
        {
            quest = new Quest();
            db.Quests.Add(quest);
        }
        else
        {
            quest = await db.Quests
                .Include(q => q.Requirements)
                .FirstOrDefaultAsync(q => q.Id == Input.Id, cancellationToken)
                ?? throw new InvalidOperationException("Quest not found");
        }

        quest.Title = Input.Title;
        quest.Description = Input.Description;
        quest.BodyContent = Input.BodyContent;
        quest.SortOrder = Input.SortOrder;
        quest.IsPublished = Input.IsPublished;
        quest.IsOnceOnly = Input.IsOnceOnly;
        quest.IsBonus = Input.IsBonus;
        quest.Points = Input.Points;

        if (IconFile is { Length: > 0 })
        {
            var uploads = Path.Combine(env.WebRootPath, "uploads");
            Directory.CreateDirectory(uploads);
            var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(IconFile.FileName)}";
            var fullPath = Path.Combine(uploads, fileName);
            await using (var stream = System.IO.File.Create(fullPath))
                await IconFile.CopyToAsync(stream, cancellationToken);
            quest.IconPath = $"/uploads/{fileName}";
        }

        await db.SaveChangesAsync(cancellationToken);

        var existing = await db.QuestRequirements
            .Where(r => r.QuestId == quest.Id)
            .ToListAsync(cancellationToken);
        db.QuestRequirements.RemoveRange(existing);

        var uniqueReqs = SelectedRequirementIds.Distinct().Where(id => id != quest.Id).ToList();
        foreach (var requiredId in uniqueReqs)
        {
            db.QuestRequirements.Add(new QuestRequirement
            {
                QuestId = quest.Id,
                RequiredQuestId = requiredId
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        TempData["StatusMessage"] = "Квест сохранён.";
        return RedirectToPage("./Index");
    }

    private async Task LoadOptionsAsync(int currentQuestId, CancellationToken cancellationToken)
    {
        RequirementOptions = await db.Quests
            .AsNoTracking()
            .Where(q => q.Id != currentQuestId)
            .OrderBy(q => q.SortOrder)
            .ThenBy(q => q.Id)
            .Select(q => new QuestOption(q.Id, q.Title))
            .ToListAsync(cancellationToken);
    }
}
