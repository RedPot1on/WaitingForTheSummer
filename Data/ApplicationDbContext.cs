using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using WaitingForTheSummer.Models;

namespace WaitingForTheSummer.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quest> Quests => Set<Quest>();
    public DbSet<QuestRequirement> QuestRequirements => Set<QuestRequirement>();
    public DbSet<GameRound> GameRounds => Set<GameRound>();
    public DbSet<Round> Rounds => Set<Round>();
    public DbSet<SideGameScore> SideGameScores => Set<SideGameScore>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<QuestRequirement>(entity =>
        {
            entity.HasIndex(x => new { x.QuestId, x.RequiredQuestId }).IsUnique();

            entity.HasOne(x => x.Quest)
                .WithMany(x => x.Requirements)
                .HasForeignKey(x => x.QuestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.RequiredQuest)
                .WithMany()
                .HasForeignKey(x => x.RequiredQuestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<GameRound>(entity =>
        {
            entity.HasIndex(x => x.Status);
            entity.HasIndex(x => x.Number).IsUnique();
            entity.HasIndex(x => x.BonusForRegularPairEnd);

            entity.HasOne(x => x.StartedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.StartedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ClosedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ClosedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Round>(entity =>
        {
            entity.HasIndex(x => new { x.GameRoundId, x.UserId }).IsUnique();
            entity.HasIndex(x => new { x.UserId, x.Status });

            entity.HasOne(x => x.GameRound)
                .WithMany(x => x.QuestTakes)
                .HasForeignKey(x => x.GameRoundId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Quest)
                .WithMany(x => x.Rounds)
                .HasForeignKey(x => x.QuestId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ResolvedByAdmin)
                .WithMany()
                .HasForeignKey(x => x.ResolvedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<SideGameScore>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.GameNumber }).IsUnique();

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
