using Microsoft.EntityFrameworkCore;

namespace CustomerCampaign.WebApi.Data;

public sealed class CampaignDbContext : DbContext
{
    public CampaignDbContext(DbContextOptions<CampaignDbContext> options) : base(options)
    {
    }

    public DbSet<RewardEntry> RewardEntries => Set<RewardEntry>();

    public DbSet<PurchaseRecord> PurchaseRecords => Set<PurchaseRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RewardEntry>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentUsername).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasIndex(e => new { e.AgentUsername, e.RewardDate });

            entity.HasIndex(e => new { e.CustomerId, e.CampaignStartDate }).IsUnique();
        });

        modelBuilder.Entity<PurchaseRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CustomerName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OrderReference).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SourceFile).IsRequired().HasMaxLength(260);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => e.CustomerId);

            entity.HasIndex(e => e.OrderReference).IsUnique();
        });
    }
}
