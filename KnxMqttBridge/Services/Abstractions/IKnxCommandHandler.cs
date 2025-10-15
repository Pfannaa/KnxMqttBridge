using KnxMqttBridge.Models;

namespace KnxMqttBridge.Services.Abstractions
{
    /// <summary>
    /// Service for handling KNX command encoding and DPT resolution
    /// </summary>
    public interface IKnxCommandHandler
    {
        /// <summary>
        /// Try to resolve the Data Point Type for a given address and command
        /// </summary>
        /// <param name="address">KNX group address (e.g., "2/1/71")</param>
        /// <param name="command">Command with optional DataPointType</param>
        /// <param name="dataPointType">Resolved DPT (e.g., "DPST-1-1")</param>
        /// <returns>True if DPT was resolved, false otherwise</returns>
        bool TryResolveDataPointType(string address, KnxCommand command, out string dataPointType);

        /// <summary>
        /// Try to encode a command value for the KNX bus
        /// </summary>
        /// <param name="commandValue">Value from command (can be number, bool, object, etc.)</param>
        /// <param name="dataPointType">Data Point Type for encoding</param>
        /// <param name="encodedValue">KNX-compatible encoded value</param>
        /// <returns>True if encoding succeeded, false otherwise</returns>
        bool TryEncodeValue(object commandValue, string dataPointType, out object encodedValue);
    }
}
