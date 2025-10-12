using Knx.Falcon;

namespace KnxMqttBridge.Services.Abstractions;

public interface IKnxService
{
    event EventHandler<GroupEventArgs> GroupMessageReceived;
    Task StartListening(CancellationToken cancellationToken);
    Task WriteAsync(string groupAddress, object value, CancellationToken cancellationToken = default);
}