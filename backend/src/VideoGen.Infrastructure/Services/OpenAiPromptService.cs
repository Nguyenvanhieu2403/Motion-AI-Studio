using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
using VideoGen.Infrastructure.Options;
namespace VideoGen.Infrastructure.Services;

/// <summary>Calls OpenAI's Responses API with both job images and returns Wan-ready English prompts.</summary>
public class OpenAiPromptService(HttpClient http, IOptions<OpenAiOptions> options) : IAiPromptService
{
    public async Task<VideoPrompts> GenerateAsync(VideoJob job, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey)) throw new InvalidOperationException("OpenAI:ApiKey is not configured.");
        var productData = $"data:image/{Path.GetExtension(job.ProductImagePath).Trim('.').Replace("jpg", "jpeg")};base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(job.ProductImagePath, ct))}";
        var referenceData = $"data:image/{Path.GetExtension(job.ReferenceImagePath).Trim('.').Replace("jpg", "jpeg")};base64,{Convert.ToBase64String(await File.ReadAllBytesAsync(job.ReferenceImagePath, ct))}";
        var instructions = "You are a commercial video prompt director. Analyze the product image and reference concept image. Return ONLY valid JSON with keys positivePrompt and negativePrompt. positivePrompt must be English, specific for Wan 2.1 image-to-video, preserve the exact product appearance, and include product type, advertising setting, camera movement, lighting, visual style, 5-10 seconds, vertical 9:16. User description: " + job.UserDescription + ". Desired style: " + job.Style + ". negativePrompt must prevent deformed product, changed text, wrong logo, distorted hands, duplicate objects, blur, flicker, watermark.";
        var body = new { model = options.Value.Model, input = new object[] { new { role = "user", content = new object[] { new { type = "input_text", text = instructions }, new { type = "input_image", image_url = productData }, new { type = "input_image", image_url = referenceData } } } }, text = new { format = new { type = "json_object" } } };
        using var request = new HttpRequestMessage(HttpMethod.Post, "v1/responses") { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
        using var response = await http.SendAsync(request, ct); var raw = await response.Content.ReadAsStringAsync(ct); response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(raw);
        var json = doc.RootElement.GetProperty("output").EnumerateArray().SelectMany(x => x.GetProperty("content").EnumerateArray()).First(x => x.GetProperty("type").GetString() == "output_text").GetProperty("text").GetString()!;
        var prompts = JsonSerializer.Deserialize<PromptJson>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("OpenAI did not return prompts.");
        return new VideoPrompts(prompts.PositivePrompt, prompts.NegativePrompt);
    }
    private record PromptJson(string PositivePrompt, string NegativePrompt);
}
