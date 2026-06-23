using Microsoft.EntityFrameworkCore;
using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
namespace VideoGen.Infrastructure.Persistence;
public class EfVideoJobRepository(VideoGenDbContext db) : IVideoJobRepository
{
    public Task AddAsync(VideoJob job, CancellationToken ct = default) => db.VideoJobs.AddAsync(job, ct).AsTask();
    public Task<VideoJob?> GetAsync(Guid id, CancellationToken ct = default) => db.VideoJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
    // This entity is intentionally tracked because the background worker advances its status in-place.
    public Task<VideoJob?> GetNextPendingAsync(CancellationToken ct = default) => db.VideoJobs.OrderBy(x => x.CreatedAt).FirstOrDefaultAsync(x => x.Status == VideoJobStatus.Pending, ct);
    public Task SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}
