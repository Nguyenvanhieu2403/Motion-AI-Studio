using VideoGen.Domain.Entities;
namespace VideoGen.Application.Abstractions;
public interface IVideoJobRepository
{
    Task AddAsync(VideoJob job, CancellationToken ct = default);
    Task<VideoJob?> GetAsync(Guid id, CancellationToken ct = default);
    Task<VideoJob?> GetNextPendingAsync(CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
