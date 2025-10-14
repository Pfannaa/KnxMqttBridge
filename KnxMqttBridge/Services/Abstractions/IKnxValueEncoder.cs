namespace KnxMqttBridge.Services.Abstractions
{
    public interface IKnxValueEncoder
    {
        /// <summary>
        /// Encodes a string payload value into the appropriate KNX value type based on the data point type.
        /// </summary>
        /// <param name="value">The string value to encode</param>
        /// <param name="dataPointType">The KNX data point type (e.g., "DPST-1-1", "DPST-3-7")</param>
        /// <returns>The encoded value (bool, byte, byte[], or tuple for special types), or null if encoding fails</returns>
        object EncodeValue(string value, string dataPointType);
    }
}
