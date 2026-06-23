using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
using VideoGen.Infrastructure.Options;
namespace VideoGen.Infrastructure.Services;

/// <summary>Submits a ComfyUI API workflow, waits for its history item, then downloads the produced MP4.</summary>
public class ComfyUiClient(HttpClient http, IOptions<ComfyUiOptions> comfy, IOptions<StorageOptions> storage, IHostEnvironment environment) : IComfyUiClient
{
    public async Task<string> RenderAsync(VideoJob job, CancellationToken ct = default)
    {
        var template = await File.ReadAllTextAsync(Path.Combine(environment.ContentRootPath, "ComfyUI", "workflows", "wan21-image-to-video.json"), ct);
        var workflow = template.Replace("{{PRODUCT_IMAGE_PATH}}", Path.GetFileName(job.ProductImagePath)).Replace("{{REFERENCE_IMAGE_PATH}}", Path.GetFileName(job.ReferenceImagePath)).Replace("{{POSITIVE_PROMPT}}", Escape(job.PositivePrompt!)).Replace("{{NEGATIVE_PROMPT}}", Escape(job.NegativePrompt!)).Replace("{{SEED}}", Random.Shared.NextInt64().ToString()).Replace("{{WIDTH}}", "576").Replace("{{HEIGHT}}", "1024").Replace("{{FRAMES}}", "121");
        using var submit = await http.PostAsync("prompt", new StringContent(JsonSerializer.Serialize(new { prompt = JsonSerializer.Deserialize<JsonElement>(workflow) }), Encoding.UTF8, "application/json"), ct);
        submit.EnsureSuccessStatusCode(); using var submitted = JsonDocument.Parse(await submit.Content.ReadAsStringAsync(ct));
        var promptId = submitted.RootElement.GetProperty("prompt_id").GetString() ?? throw new InvalidOperationException("ComfyUI response lacks prompt_id.");
        var deadline = DateTime.UtcNow.AddMinutes(comfy.Value.RenderTimeoutMinutes);
        JsonElement output = default;
        while (DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromSeconds(comfy.Value.PollIntervalSeconds), ct);
            using var historyResponse = await http.GetAsync($"history/{promptId}", ct); historyResponse.EnsureSuccessStatusCode();
            using var history = JsonDocument.Parse(await historyResponse.Content.ReadAsStringAsync(ct));
            if (!history.RootElement.TryGetProperty(promptId, out var item)) continue;
            if (item.TryGetProperty("status", out var status) && status.TryGetProperty("status_str", out var value) && value.GetString() == "error") throw new InvalidOperationException("ComfyUI render failed.");
            if (item.TryGetProperty("outputs", out output) && output.EnumerateObject().Any()) break;
        }
        if (output.ValueKind == JsonValueKind.Undefined) throw new TimeoutException("ComfyUI rendering timed out.");
        var video = output.EnumerateObject().SelectMany(x => x.Value.TryGetProperty("gifs", out var gifs) ? gifs.EnumerateArray() : x.Value.TryGetProperty("videos", out var videos) ? videos.EnumerateArray() : Enumerable.Empty<JsonElement>()).FirstOrDefault();
        if (video.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("No MP4 found in ComfyUI workflow output. Configure the SaveVideo node in the template.");
        var filename = video.GetProperty("filename").GetString()!; var subfolder = video.TryGetProperty("subfolder", out var sub) ? sub.GetString() : ""; var type = video.TryGetProperty("type", out var typeNode) ? typeNode.GetString() : "output";
        var url = $"view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder ?? "")}&type={Uri.EscapeDataString(type ?? "output")}";
        var folder = Path.Combine(environment.ContentRootPath, storage.Value.OutputFolder); Directory.CreateDirectory(folder); var target = Path.Combine(folder, $"{job.Id}.mp4");
        await using var source = await http.GetStreamAsync(url, ct); await using var destination = File.Create(target); await source.CopyToAsync(destination, ct); return target;
    }
    private static string Escape(string value) => JsonSerializer.Serialize(value).Trim('"');
}
