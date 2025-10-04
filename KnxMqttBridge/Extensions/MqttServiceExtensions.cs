using KnxMqttBridge.Services;
using System.Text.Json;

namespace KnxMqttBridge.Extensions;

public static class MqttServiceExtensions
{
    public static Task PublishKnxMessageAsync(this MqttService mqttService, string knxAddress, byte[] data, bool retain = true, CancellationToken cancellationToken = default)
    {
        var topic = $"group/{knxAddress.Replace("/", "-")}";
        return mqttService.PublishAsync(topic, data, retain, cancellationToken);
    }

    public static Task PublishKnxMessageAsJsonAsync(this MqttService mqttService, string knxAddress, object data, bool retain = true, CancellationToken cancellationToken = default)
    {
        var topic = $"group/{knxAddress.Replace("/", "-")}";
        var json = JsonSerializer.Serialize(data);
        return mqttService.PublishAsync(topic, json, retain, cancellationToken);
    }
}