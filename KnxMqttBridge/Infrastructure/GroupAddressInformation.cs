using KnxMqttBridge.Models;

namespace KnxMqttBridge.Infrastructure
{
    /// <summary>
    /// Simplified, flat representation of KNX group addresses from ETS export
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
            if (range.GroupRange == null)
            {
                return;
            }

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
        public KnxGroupAddress? GetByAddress(string address)
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
}