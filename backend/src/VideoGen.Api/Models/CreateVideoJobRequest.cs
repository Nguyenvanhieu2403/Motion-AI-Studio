namespace VideoGen.Api.Models;
public class CreateVideoJobRequest
{
    public IFormFile? ProductImage { get; set; }
    public IFormFile? ReferenceImage { get; set; }
    public string? UserDescription { get; set; }
    public string? Style { get; set; }
}
public record VideoJobResponse(Guid Id, string Status, string? PositivePrompt, string? NegativePrompt, string? ErrorMessage, DateTime CreatedAt, DateTime UpdatedAt, string? VideoUrl);
