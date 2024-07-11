using System.Text.Encodings.Web;
using System.Text.Json;

namespace ImageClassifier.Shared;

public sealed class AppConfig
{
    private static readonly NLog.Logger _logger = NLog.LogManager.GetCurrentClassLogger();

    public string? SourcePath { get; set; }
    public string? LeftPath { get; set; }
    public string? RightPath { get; set; }
    public string? DownPath { get; set; }

    public static async ValueTask<AppConfig> LoadAsync(string configPath)
    {
        AppConfig? result = null;

        try
        {
            var options = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            using var stream = new FileStream(configPath, FileMode.Open);
            result = JsonSerializer.Deserialize<AppConfig>(stream, options);
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unexpected Exception");
        }

        result ??= new AppConfig();

        return result;
    }
}
