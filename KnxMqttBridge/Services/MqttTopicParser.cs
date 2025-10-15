using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KnxMqttBridge.Services
{
    /// <summary>
    /// Service for parsing MQTT topics and deserializing commands
    /// </summary>
    public class MqttTopicParser : IMqttTopicParser
    {
        private readonly ILogger<MqttTopicParser> _logger;
        private readonly KnxConfiguration _knxConfiguration;

        public MqttTopicParser(
            ILogger<MqttTopicParser> logger,
            IOptions<KnxConfiguration> knxConfiguration)
        {
            _logger = logger;
            _knxConfiguration = knxConfiguration.Value;
        }

        public bool TryParseAddressFromTopic(string topic, out string address)
        {
            address = string.Empty;
            var parts = topic.Split('/');

            if (_knxConfiguration.AddressStyle == KnxAddressStyle.TwoLevel)
            {
                // Parse format: knx/GroupAddresses/{main}/{sub}/command
                if (parts.Length != 5 || parts[0] != "knx" || parts[1] != "GroupAddresses" || parts[4] != "command")
                {
                    _logger.LogWarning("Invalid command topic format: {Topic}. Expected knx/GroupAddresses/{{main}}/{{sub}}/command", topic);
                    return false;
                }

                address = $"{parts[2]}/{parts[3]}";
            }
            else
            {
                // Parse format: knx/GroupAddresses/{main}/{middle}/{sub}/command
                if (parts.Length != 6 || parts[0] != "knx" || parts[1] != "GroupAddresses" || parts[5] != "command")
                {
                    _logger.LogWarning("Invalid command topic format: {Topic}. Expected knx/GroupAddresses/{{main}}/{{middle}}/{{sub}}/command", topic);
                    return false;
                }

                address = $"{parts[2]}/{parts[3]}/{parts[4]}";
            }

            return true;
        }

        public bool TryDeserializeCommand(string payload, out KnxCommand command)
        {
            command = null!;

            try
            {
                command = JsonSerializer.Deserialize<KnxCommand>(payload);
                if (command == null)
                {
                    _logger.LogWarning("Failed to deserialize command payload: {Payload}", payload);
                    return false;
                }
                return true;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid JSON command payload: {Payload}", payload);
                return false;
            }
        }
    }
}
