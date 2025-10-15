namespace KnxMqttBridge.Models
{
    /// <summary>
    /// Represents a command to send to the KNX bus via MQTT
    /// </summary>
    public class KnxCommand
    {
        /// <summary>
        /// The value to send (can be bool, number, string, or object for complex types like dimming)
        /// </summary>
        public required object Value { get; set; }

        /// <summary>
        /// Optional: Data Point Type (e.g., "DPST-1-1", "DPST-5-1")
        /// Only needed if the address is not in the ETS configuration
        /// </summary>
        public string? DataPointType { get; set; }
    }
}
