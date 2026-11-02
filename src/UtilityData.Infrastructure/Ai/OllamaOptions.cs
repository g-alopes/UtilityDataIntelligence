namespace UtilityData.Infrastructure.Ai;
public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string Model { get; set; } = "qwen2.5:1.5b";
    public int TimeoutSeconds { get; set; } = 120;
}
