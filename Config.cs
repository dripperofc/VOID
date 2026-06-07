using System.IO;
using System.Text.Json;

namespace Void;

public static class Config
{
    private const string DefaultServerUrl = "https://void-server-gkjx.onrender.com";
    private const string ConfigFileName = "config.json";

    private static string? _serverUrl;
    private static bool _loaded;

    public static string ServerUrl
    {
        get
        {
            if (!_loaded) Load();
            return _serverUrl ?? DefaultServerUrl;
        }
    }

    private static void Load()
    {
        _loaded = true;
        try
        {
            if (!File.Exists(ConfigFileName)) return;
            var json = File.ReadAllText(ConfigFileName);
            var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty("serverUrl", out var url))
                _serverUrl = url.GetString();
        }
        catch
        {
            // fallback silencioso pro default
        }
    }
}
