using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VideoGen.Api.Models;
using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
using VideoGen.Infrastructure.Options;
namespace VideoGen.Api.Controllers;

[ApiController, Route("api/video-jobs")]
public class VideoJobsController(IVideoJobRepository repo, IOptions<StorageOptions> storage, IHostEnvironment environment) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxFileBytes = 10 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<ActionResult<VideoJobResponse>> Create([FromForm] CreateVideoJobRequest request, CancellationToken ct)
    {
        if (request.ProductImage is null || request.ReferenceImage is null) return BadRequest("ProductImage and ReferenceImage are required.");
        if (string.IsNullOrWhiteSpace(request.UserDescription)) return BadRequest("UserDescription is required.");
        var product = await SaveImageAsync(request.ProductImage, ct); var reference = await SaveImageAsync(request.ReferenceImage, ct);
        var job = new VideoJob { ProductImagePath = product, ReferenceImagePath = reference, UserDescription = request.UserDescription.Trim(), Style = request.Style?.Trim() ?? string.Empty };
        await repo.AddAsync(job, ct); await repo.SaveChangesAsync(ct); return AcceptedAtAction(nameof(Get), new { id = job.Id }, ToResponse(job));
    }
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<VideoJobResponse>> Get(Guid id, CancellationToken ct) => (await repo.GetAsync(id, ct)) is { } job ? Ok(ToResponse(job)) : NotFound();
    [HttpGet("{id:guid}/video")]
    public async Task<IActionResult> Video(Guid id, CancellationToken ct)
    {
        var job = await repo.GetAsync(id, ct); if (job?.Status != VideoJobStatus.Completed || string.IsNullOrEmpty(job.OutputVideoPath) || !System.IO.File.Exists(job.OutputVideoPath)) return NotFound();
        return PhysicalFile(job.OutputVideoPath, "video/mp4", enableRangeProcessing: true);
    }
    private async Task<string> SaveImageAsync(IFormFile file, CancellationToken ct)
    {
        var extension = Path.GetExtension(file.FileName); if (file.Length is 0 or > MaxFileBytes || !AllowedExtensions.Contains(extension)) throw new BadHttpRequestException("Images must be JPG, JPEG, PNG or WEBP and no larger than 10 MB.");
        if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp")) throw new BadHttpRequestException("Invalid image content type.");
        var folder = Path.Combine(environment.ContentRootPath, storage.Value.UploadFolder); Directory.CreateDirectory(folder); var target = Path.Combine(folder, $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}");
        await using var output = System.IO.File.Create(target); await file.CopyToAsync(output, ct); return target;
    }
    private VideoJobResponse ToResponse(VideoJob j) => new(j.Id, j.Status.ToString(), j.PositivePrompt, j.NegativePrompt, j.ErrorMessage, j.CreatedAt, j.UpdatedAt, j.Status == VideoJobStatus.Completed ? Url.ActionLink(nameof(Video), values: new { id = j.Id }) : null);
}
