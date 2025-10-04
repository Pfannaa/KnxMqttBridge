using System;
using System.Collections.Generic;
using System.Linq;

namespace KnxMqttBridge.Infrastructure
{
    /// <summary>
    /// Simplified, flat representation of KNX group addresses
    /// </summary>
    public class GroupAddressInformation
    {
        public List<KnxGroupAddress> GroupAddresses { get; set; } = new();

        /// <summary>
        /// Creates a simplified configuration from the ugly XML export
        /// </summary>
        public static GroupAddressInformation FromXmlExport(GroupAddressExport export)
        {
            var config = new GroupAddressInformation();

            foreach (var topRange in export.GroupRange ?? Enumerable.Empty<GroupAddressExportGroupRange>())
            {
                ProcessGroupRange(topRange, config.GroupAddresses, topRange.Name);
            }

            return config;
        }

        private static void ProcessGroupRange(
            GroupAddressExportGroupRange range,
            List<KnxGroupAddress> addresses,
            string parentPath)
        {
            if (range.GroupRange == null) return;

            foreach (var subRange in range.GroupRange)
            {
                var currentPath = $"{parentPath}/{subRange.Name}";

                // Add all group addresses from this range
                if (subRange.GroupAddress != null)
                {
                    foreach (var ga in subRange.GroupAddress)
                    {
                        addresses.Add(new KnxGroupAddress
                        {
                            Name = ga.Name,
                            Address = ga.Address,
                            DataPointType = ga.DPTs,
                            Category = parentPath,
                            Subcategory = subRange.Name,
                            FullPath = currentPath,
                            Security = ga.Security
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Find a group address by its KNX address (e.g. "2/1/1")
        /// </summary>
        public KnxGroupAddress GetByAddress(string address)
        {
            return GroupAddresses.FirstOrDefault(ga => ga.Address == address);
        }

        /// <summary>
        /// Find all group addresses in a category
        /// </summary>
        public IEnumerable<KnxGroupAddress> GetByCategory(string category)
        {
            return GroupAddresses.Where(ga => ga.Category == category);
        }

        /// <summary>
        /// Find all group addresses with a specific data point type
        /// </summary>
        public IEnumerable<KnxGroupAddress> GetByDataPointType(string dpt)
        {
            return GroupAddresses.Where(ga => ga.DataPointType == dpt);
        }
    }

    public class KnxGroupAddress
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string DataPointType { get; set; }
        public string Category { get; set; }
        public string Subcategory { get; set; }
        public string FullPath { get; set; }
        public string Security { get; set; }

        // Runtime state - actual value on the bus
        public object Value { get; set; }
        public byte[] RawValue { get; set; }
        public DateTime? LastUpdated { get; set; }

        /// <summary>
        /// Get the human-readable value based on the data point type
        /// </summary>
        public object GetDecodedValue()
        {
            if (RawValue == null || RawValue.Length == 0)
            {
                return null;
            }

            return DataPointType switch
            {
                "DPST-1-1" or "DPST-1-11" or "DPST-1-24" => (int)(RawValue[0] != 0 ? 1 : 0), // Boolean (switch, status, etc)
                "DPST-5-1" => RawValue[0], // Unsigned 8-bit (0-255, e.g. brightness percentage)
                "DPST-9-1" => DecodeFloat16(RawValue), // 2-byte float (temperature)
                "DPST-3-7" => new { Direction = (RawValue[0] & 0x08) != 0 ? "up" : "down", Steps = RawValue[0] & 0x07 }, // Dimming control
                "DPST-10-1" => DecodeTime(RawValue), // Time
                "DPST-11-1" => DecodeDate(RawValue), // Date
                "DPST-18-1" => RawValue[0], // Scene number
                _ => Convert.ToBase64String(RawValue) // Fallback to base64 for unknown types
            };
        }

        private static float DecodeFloat16(byte[] data)
        {
            if (data.Length < 2) return 0;

            int value = (data[0] << 8) | data[1];
            int sign = (value & 0x8000) >> 15;
            int exponent = (value & 0x7800) >> 11;
            int mantissa = value & 0x07FF;

            float result = (1 << exponent) * (mantissa / 100.0f);
            return sign == 1 ? -result : result;
        }

        private static string DecodeTime(byte[] data)
        {
            if (data.Length < 3) return null;
            return $"{data[0] & 0x1F:D2}:{data[1] & 0x3F:D2}:{data[2] & 0x3F:D2}";
        }

        private static string DecodeDate(byte[] data)
        {
            if (data.Length < 3) return null;
            int day = data[0] & 0x1F;
            int month = data[1] & 0x0F;
            int year = 2000 + (data[2] & 0x7F);
            return $"{year:D4}-{month:D2}-{day:D2}";
        }

        /// <summary>
        /// Parse the KNX address into its components (main/middle/sub)
        /// </summary>
        public (int Main, int Middle, int Sub) ParseAddress()
        {
            var parts = Address.Split('/');
            return (
                int.Parse(parts[0]),
                int.Parse(parts[1]),
                int.Parse(parts[2])
            );
        }

        /// <summary>
        /// Check if this is a write address (ends with 's' or 'wr')
        /// </summary>
        public bool IsWriteAddress()
        {
            return Name.EndsWith(" s") || Name.EndsWith(" wr");
        }

        /// <summary>
        /// Check if this is a read/status address (ends with 'sr' or 'r')
        /// </summary>
        public bool IsReadAddress()
        {
            return Name.EndsWith(" sr") || Name.EndsWith(" r");
        }

        public override string ToString()
        {
            return $"{Address} - {Name} ({DataPointType})";
        }
    }
}