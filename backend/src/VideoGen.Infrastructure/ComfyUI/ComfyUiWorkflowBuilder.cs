using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Hosting;
namespace VideoGen.Infrastructure.ComfyUI;

/// <summary>
/// Builds a ComfyUI API-format graph from the checked-in template. String replacement is JSON-safe
/// and the resulting graph is parsed before it is ever sent to /prompt.
/// </summary>
public sealed class ComfyUiWorkflowBuilder(IHostEnvironment environment)
{
    private const string RelativeTemplatePath = "ComfyUI/workflows/wan21-image-to-video.json";
    private static readonly string[] Tokens = ["{{PRODUCT_IMAGE_PATH}}", "{{REFERENCE_IMAGE_PATH}}", "{{POSITIVE_PROMPT}}", "{{NEGATIVE_PROMPT}}", "{{SEED}}", "{{WIDTH}}", "{{HEIGHT}}", "{{FRAMES}}"];

    public async Task<JsonObject> BuildAsync(ComfyUiWorkflowInput input, CancellationToken ct = default)
    {
        ValidateInput(input);
        var templatePath = Path.Combine(environment.ContentRootPath, RelativeTemplatePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(templatePath)) throw new FileNotFoundException("ComfyUI workflow template was not found.", templatePath);
        var json = await File.ReadAllTextAsync(templatePath, ct);

        // Replace the whole quoted token with serialized JSON to preserve quotes, Unicode and newlines in prompts.
        json = json.Replace("\"{{PRODUCT_IMAGE_PATH}}\"", JsonSerializer.Serialize(Path.GetFileName(input.ProductImagePath)))
                   .Replace("\"{{REFERENCE_IMAGE_PATH}}\"", JsonSerializer.Serialize(Path.GetFileName(input.ReferenceImagePath)))
                   .Replace("\"{{POSITIVE_PROMPT}}\"", JsonSerializer.Serialize(input.PositivePrompt))
                   .Replace("\"{{NEGATIVE_PROMPT}}\"", JsonSerializer.Serialize(input.NegativePrompt))
                   .Replace("{{SEED}}", input.Seed.ToString(System.Globalization.CultureInfo.InvariantCulture))
                   .Replace("{{WIDTH}}", input.Width.ToString(System.Globalization.CultureInfo.InvariantCulture))
                   .Replace("{{HEIGHT}}", input.Height.ToString(System.Globalization.CultureInfo.InvariantCulture))
                   .Replace("{{FRAMES}}", input.Frames.ToString(System.Globalization.CultureInfo.InvariantCulture));
        var remaining = Tokens.FirstOrDefault(json.Contains);
        if (remaining is not null) throw new InvalidOperationException($"Workflow contains unreplaced placeholder {remaining}.");
        try
        {
            var graph = JsonNode.Parse(json) as JsonObject ?? throw new InvalidOperationException("ComfyUI workflow root must be a JSON object.");
            if (graph.Count == 0 || graph.Any(n => n.Value is not JsonObject node || node["class_type"] is null || node["inputs"] is not JsonObject))
                throw new InvalidOperationException("Workflow must be ComfyUI API Format: every node needs class_type and inputs.");
            return graph;
        }
        catch (JsonException ex) { throw new InvalidOperationException("Rendered ComfyUI workflow is invalid JSON.", ex); }
    }

    private static void ValidateInput(ComfyUiWorkflowInput input)
    {
        if (string.IsNullOrWhiteSpace(input.ProductImagePath) || string.IsNullOrWhiteSpace(input.ReferenceImagePath)) throw new ArgumentException("Both input image paths are required.");
        if (string.IsNullOrWhiteSpace(input.PositivePrompt) || string.IsNullOrWhiteSpace(input.NegativePrompt)) throw new ArgumentException("Both prompts are required.");
        if (input.Width <= 0 || input.Height <= 0 || input.Frames <= 0) throw new ArgumentOutOfRangeException(nameof(input), "Dimensions and frames must be positive.");
    }
}

public sealed record ComfyUiWorkflowInput(string ProductImagePath, string ReferenceImagePath, string PositivePrompt, string NegativePrompt, long Seed, int Width, int Height, int Frames);
