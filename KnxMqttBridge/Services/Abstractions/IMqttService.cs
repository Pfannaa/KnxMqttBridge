namespace KnxMqttBridge.Services.Abstractions;

public interface IMqttService
{
    bool IsConnected { get; }
    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default);
    Task PublishAsync(string topic, byte[] payload, bool retain = false, CancellationToken cancellationToken = default);
}