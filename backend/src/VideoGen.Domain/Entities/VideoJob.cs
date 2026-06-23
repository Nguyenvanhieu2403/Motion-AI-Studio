namespace VideoGen.Domain.Entities;

public enum VideoJobStatus { Pending, Analyzing, PromptGenerated, Rendering, Completed, Failed }

/// <summary>Persistent record for a product-video generation request.</summary>
public class VideoJob
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ProductImagePath { get; set; } = null!;
    public string ReferenceImagePath { get; set; } = null!;
    public string UserDescription { get; set; } = string.Empty;
    public string Style { get; set; } = string.Empty;
    public string? PositivePrompt { get; set; }
    public string? NegativePrompt { get; set; }
    public VideoJobStatus Status { get; set; } = VideoJobStatus.Pending;
    public string? OutputVideoPath { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
