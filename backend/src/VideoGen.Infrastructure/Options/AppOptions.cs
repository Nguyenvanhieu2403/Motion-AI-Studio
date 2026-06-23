namespace VideoGen.Infrastructure.Options;
public class OpenAiOptions { public string ApiKey { get; set; } = string.Empty; public string Model { get; set; } = "gpt-4o-mini"; }
public class ComfyUiOptions { public string BaseUrl { get; set; } = "http://host.docker.internal:8188"; public int PollIntervalSeconds { get; set; } = 3; public int RenderTimeoutMinutes { get; set; } = 20; }
public class StorageOptions { public string UploadFolder { get; set; } = "uploads"; public string OutputFolder { get; set; } = "outputs"; }
