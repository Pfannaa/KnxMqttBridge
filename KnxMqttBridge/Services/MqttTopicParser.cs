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
        private readonly MqttConfiguration _mqttConfiguration;

        public MqttTopicParser(
            ILogger<MqttTopicParser> logger,
            IOptions<KnxConfiguration> knxConfiguration,
            IOptions<MqttConfiguration> mqttConfiguration)
        {
            _logger = logger;
            _knxConfiguration = knxConfiguration.Value;
            _mqttConfiguration = mqttConfiguration.Value;
        }

        public bool TryParseAddressFromTopic(string topic, out string address)
        {
            address = string.Empty;
            var topicPrefix = _mqttConfiguration.TopicPrefix ?? "knx";

            // Expected format: {prefix}/GroupAddresses/{address}/command
            var expectedPrefix = topicPrefix + "/GroupAddresses/";

            if (!topic.StartsWith(expectedPrefix))
            {
                _logger.LogWarning("Topic does not start with expected prefix: {Topic}. Expected: {ExpectedPrefix}...", topic, expectedPrefix);
                return false;
            }

            if (!topic.EndsWith("/command"))
            {
                _logger.LogWarning("Topic does not end with /command: {Topic}", topic);
                return false;
            }

            // Extract the address part between prefix and /command
            var startIndex = expectedPrefix.Length;
            var endIndex = topic.LastIndexOf("/command");
            var addressPart = topic.Substring(startIndex, endIndex - startIndex);

            // Validate address format based on style
            var addressParts = addressPart.Split('/');

            if (_knxConfiguration.AddressStyle == KnxAddressStyle.TwoLevel)
            {
                // Expected: {main}/{sub}
                if (addressParts.Length != 2)
                {
                    _logger.LogWarning("Invalid two-level address format: {Address}. Expected {{main}}/{{sub}}", addressPart);
                    return false;
                }
            }
            else
            {
                // Expected: {main}/{middle}/{sub}
                if (addressParts.Length != 3)
                {
                    _logger.LogWarning("Invalid three-level address format: {Address}. Expected {{main}}/{{middle}}/{{sub}}", addressPart);
                    return false;
                }
            }

            address = addressPart;
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
