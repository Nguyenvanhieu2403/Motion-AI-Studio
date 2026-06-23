using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
namespace VideoGen.Api.Services;

/// <summary>Processes one pending request at a time so the API stays responsive while ComfyUI renders.</summary>
public class VideoJobWorker(IServiceScopeFactory scopes, ILogger<VideoJobWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopes.CreateScope(); var repo = scope.ServiceProvider.GetRequiredService<IVideoJobRepository>();
                var job = await repo.GetNextPendingAsync(stoppingToken);
                if (job is null) { await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken); continue; }
                try
                {
                    job.Status = VideoJobStatus.Analyzing; job.UpdatedAt = DateTime.UtcNow; await repo.SaveChangesAsync(stoppingToken);
                    var prompts = await scope.ServiceProvider.GetRequiredService<IAiPromptService>().GenerateAsync(job, stoppingToken);
                    job.PositivePrompt = prompts.Positive; job.NegativePrompt = prompts.Negative; job.Status = VideoJobStatus.PromptGenerated; job.UpdatedAt = DateTime.UtcNow; await repo.SaveChangesAsync(stoppingToken);
                    job.Status = VideoJobStatus.Rendering; job.UpdatedAt = DateTime.UtcNow; await repo.SaveChangesAsync(stoppingToken);
                    job.OutputVideoPath = await scope.ServiceProvider.GetRequiredService<IComfyUiClient>().RenderAsync(job, stoppingToken);
                    job.Status = VideoJobStatus.Completed; job.UpdatedAt = DateTime.UtcNow; await repo.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
                {
                    logger.LogError(ex, "Video job {JobId} failed", job.Id);
                    job.Status = VideoJobStatus.Failed; job.ErrorMessage = ex.Message; job.UpdatedAt = DateTime.UtcNow; await repo.SaveChangesAsync(CancellationToken.None);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "Video job processing failed");
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
