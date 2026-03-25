using System.Text.Json;
using KnxMqttBridge.Web.Infrastructure;
using KnxMqttBridge.Web.Models;
using Microsoft.Extensions.Options;

namespace KnxMqttBridge.Web.Services;

public class ConfigService(IOptions<WebConfiguration> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private string ConfigPath => options.Value.ConfigPath;

    public AppConfig Load()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        try
        {
            var json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
        }
        catch
        {
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
