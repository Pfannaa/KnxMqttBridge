namespace KnxMqttBridge.Infrastructure;

public class MqttConfiguration
{
    public string BrokerHost { get; set; } = "localhost";
    public int BrokerPort { get; set; } = 1883;
    public string ClientId { get; set; } = "knx-mqtt-bridge";
    public string Username { get; set; }
    public string Password { get; set; }
    public string TopicPrefix { get; set; } = "knx";
    public bool CleanSession { get; set; } = true;
    public int KeepAlivePeriod { get; set; } = 60;
}