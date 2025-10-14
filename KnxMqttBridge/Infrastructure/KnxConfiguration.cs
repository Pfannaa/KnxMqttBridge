namespace KnxMqttBridge.Infrastructure
{
    public class KnxConfiguration
    {
        public string? XmlPath { get; set; } = "GroupAddresses.xml";
        public string? GatewayIp { get; set; }
        public int GatewayPort { get; set; } = 3671;
        public bool UseAutoDiscovery { get; set; } = true;
    }
}
