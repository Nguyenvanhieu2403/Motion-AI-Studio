using VideoGen.Domain.Entities;
namespace VideoGen.Application.Abstractions;
public interface IComfyUiClient { Task<string> RenderAsync(VideoJob job, CancellationToken ct = default); }
