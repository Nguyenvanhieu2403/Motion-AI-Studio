using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using VideoGen.Application.Abstractions;
using VideoGen.Domain.Entities;
using VideoGen.Infrastructure.ComfyUI;
using VideoGen.Infrastructure.Options;
namespace VideoGen.Infrastructure.Services;

/// <summary>Submits a ComfyUI API workflow, waits for its history item, then downloads the produced MP4.</summary>
public class ComfyUiClient(HttpClient http, IOptions<ComfyUiOptions> comfy, IOptions<StorageOptions> storage, IHostEnvironment environment, ComfyUiWorkflowBuilder workflowBuilder) : IComfyUiClient
{
    public async Task<string> RenderAsync(VideoJob job, CancellationToken ct = default)
    {
        // 576×1024 at 16 fps for 121 frames is ~7.6 seconds: ideal for a 9:16 product reel.
        var workflow = await workflowBuilder.BuildAsync(new(job.ProductImagePath, job.ReferenceImagePath, job.PositivePrompt!, job.NegativePrompt!, Random.Shared.NextInt64(), 576, 1024, 121), ct);
        using var submit = await http.PostAsync("prompt", new StringContent(JsonSerializer.Serialize(new { prompt = workflow }), Encoding.UTF8, "application/json"), ct);
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
        var video = output.EnumerateObject().SelectMany(x => ReadOutputFiles(x.Value, "videos").Concat(ReadOutputFiles(x.Value, "gifs"))).FirstOrDefault();
        if (video.ValueKind == JsonValueKind.Undefined) throw new InvalidOperationException("No MP4 found in ComfyUI workflow output. Configure the SaveVideo node in the template.");
        var filename = video.GetProperty("filename").GetString()!; var subfolder = video.TryGetProperty("subfolder", out var sub) ? sub.GetString() : ""; var type = video.TryGetProperty("type", out var typeNode) ? typeNode.GetString() : "output";
        var url = $"view?filename={Uri.EscapeDataString(filename)}&subfolder={Uri.EscapeDataString(subfolder ?? "")}&type={Uri.EscapeDataString(type ?? "output")}";
        var folder = Path.Combine(environment.ContentRootPath, storage.Value.OutputFolder); Directory.CreateDirectory(folder); var target = Path.Combine(folder, $"{job.Id}.mp4");
        await using var source = await http.GetStreamAsync(url, ct); await using var destination = File.Create(target); await source.CopyToAsync(destination, ct); return target;
    }
    private static IEnumerable<JsonElement> ReadOutputFiles(JsonElement node, string name) => node.TryGetProperty(name, out var files) && files.ValueKind == JsonValueKind.Array ? files.EnumerateArray() : Enumerable.Empty<JsonElement>();
}
