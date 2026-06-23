using Microsoft.EntityFrameworkCore;
using VideoGen.Domain.Entities;
namespace VideoGen.Infrastructure.Persistence;
public class VideoGenDbContext(DbContextOptions<VideoGenDbContext> options) : DbContext(options)
{
    public DbSet<VideoJob> VideoJobs => Set<VideoJob>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<VideoJob>();
        job.ToTable("VideoJobs"); job.HasKey(x => x.Id);
        job.Property(x => x.Status).HasConversion<string>().HasMaxLength(32);
        job.Property(x => x.ProductImagePath).HasMaxLength(500); job.Property(x => x.ReferenceImagePath).HasMaxLength(500);
        job.Property(x => x.PositivePrompt).HasColumnType("text"); job.Property(x => x.NegativePrompt).HasColumnType("text");
        job.HasIndex(x => new { x.Status, x.CreatedAt });
    }
}
