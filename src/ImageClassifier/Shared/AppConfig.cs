using System.Text.Encodings.Web;
using System.Text.Json;

namespace ImageClassifier.Shared;

public sealed class AppConfig
{
    public string? SourcePath { get; set; }
    public string? LeftPath { get; set; }
    public string? RightPath { get; set; }
    public string? DownPath { get; set; }

    public static async ValueTask<AppConfig?> LoadAsync(string configPath)
    {
        var options = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        using var stream = new FileStream(configPath, FileMode.Open);
        return JsonSerializer.Deserialize<AppConfig>(stream, options);
    }
}
