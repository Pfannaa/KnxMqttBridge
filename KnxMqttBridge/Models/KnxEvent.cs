namespace KnxMqttBridge.Models
{
    /// <summary>
    /// Represents a KNX telegram event with core data and optional metadata
    /// </summary>
    public class KnxEvent
    {
        /// <summary>
        /// KNX group address (e.g., "2/1/71")
        /// </summary>
        public required string Address { get; set; }

        /// <summary>
        /// Raw byte value from the KNX telegram (base64 encoded when serialized)
        /// </summary>
        public required byte[] RawValue { get; set; }

        /// <summary>
        /// Decoded value (type depends on DPT or heuristic)
        /// </summary>
        public required object Value { get; set; }

        /// <summary>
        /// Timestamp when the telegram was received
        /// </summary>
        public required DateTime Timestamp { get; set; }

        /// <summary>
        /// Optional metadata from ETS project (only available if address is configured)
        /// </summary>
        public KnxMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Optional metadata about a KNX group address from ETS export
    /// </summary>
    public class KnxMetadata
    {
        /// <summary>
        /// Human-readable name from ETS (e.g., "Büro Decke Licht s")
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Category/main group (e.g., "Schalten Dimmen")
        /// </summary>
        public required string Category { get; set; }

        /// <summary>
        /// Subcategory/middle group (e.g., "Schalten Dimmen")
        /// </summary>
        public required string Subcategory { get; set; }

        /// <summary>
        /// Full hierarchical path (e.g., "Schalten Dimmen/Schalten Dimmen")
        /// </summary>
        public required string FullPath { get; set; }

        /// <summary>
        /// KNX data point type (e.g., "DPST-1-1")
        /// </summary>
        public required string DataPointType { get; set; }

        /// <summary>
        /// Human-readable description of the data point type
        /// </summary>
        public string? DataPointDescription { get; set; }

        /// <summary>
        /// Classic programming type (bool, int, float, string, etc.)
        /// </summary>
        public string? ClassicDataType { get; set; }

        /// <summary>
        /// Security settings from ETS
        /// </summary>
        public string? Security { get; set; }
    }
}
