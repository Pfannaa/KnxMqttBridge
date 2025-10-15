using System.Text.Json.Serialization;

namespace KnxMqttBridge.Infrastructure
{
    /// <summary>
    /// Defines the KNX group address format style
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KnxAddressStyle
    {
        /// <summary>
        /// Two-level addressing: Main/Sub (e.g., 1/71)
        /// </summary>
        TwoLevel,

        /// <summary>
        /// Three-level addressing: Main/Middle/Sub (e.g., 2/1/71)
        /// </summary>
        ThreeLevel
    }
}
