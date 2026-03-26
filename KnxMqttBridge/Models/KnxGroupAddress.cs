namespace KnxMqttBridge.Models
{
    /// <summary>
    /// Represents a KNX group address configuration from ETS export
    /// </summary>
    public class KnxGroupAddress
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string DataPointType { get; set; }
        public required string Category { get; set; }
        public required string Subcategory { get; set; }
        public required string FullPath { get; set; }
        public string? Security { get; set; }

        /// <summary>
        /// Parse the KNX address into its components.
        /// For three-level addresses (main/middle/sub), Middle is populated.
        /// For two-level addresses (main/sub), Middle is null.
        /// </summary>
        public (int Main, int? Middle, int Sub) ParseAddress()
        {
            var parts = Address.Split('/');
            if (parts.Length == 2)
            {
                return (int.Parse(parts[0]), null, int.Parse(parts[1]));
            }
            return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
        }

        public override string ToString()
        {
            return $"{Address} - {Name} ({DataPointType})";
        }
    }
}
