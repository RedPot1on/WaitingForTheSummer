namespace WaitingForTheSummer.Models;

public class QuestRequirement
{
    public int Id { get; set; }

    public int QuestId { get; set; }
    public Quest Quest { get; set; } = null!;

    public int RequiredQuestId { get; set; }
    public Quest RequiredQuest { get; set; } = null!;
}
