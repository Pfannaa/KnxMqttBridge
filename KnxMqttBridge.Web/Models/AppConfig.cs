namespace KnxMqttBridge.Web.Models;

public class AppConfig
{
    public FrontendSettings Settings { get; set; } = new();
    public UiConfig UiConfig { get; set; } = new();
}

public class FrontendSettings
{
    public string MqttBrokerHost { get; set; } = "localhost";
    public int MqttWebSocketPort { get; set; } = 9001;
    public string MqttUsername { get; set; } = "";
    public string MqttPassword { get; set; } = "";
    public string TopicPrefix { get; set; } = "knx";
    public string AddressStyle { get; set; } = "ThreeLevel";
}

public class UiConfig
{
    public List<string> Groups { get; set; } = [];
    public List<UiConfigItem> Items { get; set; } = [];
}

public class UiConfigItem
{
    public required string Address { get; set; }
    public string? Label { get; set; }
    public string? Group { get; set; }
    public int Order { get; set; }
}
