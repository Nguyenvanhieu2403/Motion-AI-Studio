using VideoGen.Domain.Entities;
namespace VideoGen.Application.Abstractions;
public record VideoPrompts(string Positive, string Negative);
public interface IAiPromptService { Task<VideoPrompts> GenerateAsync(VideoJob job, CancellationToken ct = default); }
