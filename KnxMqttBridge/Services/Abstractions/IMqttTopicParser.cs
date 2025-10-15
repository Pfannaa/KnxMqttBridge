using KnxMqttBridge.Models;

namespace KnxMqttBridge.Services.Abstractions
{
    /// <summary>
    /// Service for parsing MQTT topics and deserializing commands
    /// </summary>
    public interface IMqttTopicParser
    {
        /// <summary>
        /// Try to parse a KNX address from an MQTT command topic
        /// </summary>
        /// <param name="topic">MQTT topic (e.g., "knx/2/1/71/command")</param>
        /// <param name="address">Extracted KNX address (e.g., "2/1/71")</param>
        /// <returns>True if parsing succeeded, false otherwise</returns>
        bool TryParseAddressFromTopic(string topic, out string address);

        /// <summary>
        /// Try to deserialize a JSON command payload
        /// </summary>
        /// <param name="payload">JSON string</param>
        /// <param name="command">Deserialized command object</param>
        /// <returns>True if deserialization succeeded, false otherwise</returns>
        bool TryDeserializeCommand(string payload, out KnxCommand command);
    }
}
