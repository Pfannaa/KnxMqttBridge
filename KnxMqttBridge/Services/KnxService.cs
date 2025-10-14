using Knx.Falcon.Configuration;
using Knx.Falcon.Discovery;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using KnxMqttBridge.Infrastructure;
using Microsoft.Extensions.Options;
using Knx.Falcon;
using KnxMqttBridge.Services.Abstractions;

namespace KnxMqttBridge.Services
{
    internal class KnxService : IKnxService
    {
        public event EventHandler<GroupEventArgs> GroupMessageReceived;

        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;
        private KnxBus _bus;

        // X1 IP 192.168.1.169

        public KnxService(IOptions<GroupAddressInformation> groupAddressInformation)
        {
            _groupAddressInformation = groupAddressInformation;
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            var ipDiscovery = new IpDeviceDiscovery
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
            var ipDeviceDiscoveryTask = ipDiscovery.DiscoverAsync(cancellationToken);

            var ipDevices = await ipDeviceDiscoveryTask
                .Where(_ => _.Supports(ServiceFamily.Tunneling, 1), cancellationToken)
                .ToArray(CancellationToken.None);

            List<string> connectionStrings = new List<string>();

            if (ipDevices.Any())
            {
                foreach (var tunnelingServer in ipDevices.SelectMany(ipDevice => ipDevice.GetTunnelingConnections()))
                {
                    connectionStrings.Add(tunnelingServer.ToConnectionString());
                }
            }

            if (connectionStrings.Count < 1)
            {
                Console.WriteLine("Failed to find valid connection string. Exiting...");
                Environment.Exit(1);
            }

            var connectorParameters = ConnectorParameters.FromConnectionString(connectionStrings[0]);

            Console.WriteLine($"[KnxService] Using connection string: {connectionStrings[0]}");

            _bus = new KnxBus(connectorParameters);
            await _bus.ConnectAsync(cancellationToken);

            Console.WriteLine($"[KnxService] Connected to KNX bus successfully");

            _bus.GroupMessageReceived += (sender, args) =>
            {
                Console.WriteLine($"[KnxService] Received telegram - Dest: {args.DestinationAddress}, Source: {args.SourceAddress}, Type: {args.EventType}, Value: {BitConverter.ToString(args.Value.Value)}");
                GroupMessageReceived?.Invoke(this, args);
            };
        }

        public async Task WriteAsync(string groupAddress, object value, CancellationToken cancellationToken = default)
        {
            if (_bus == null)
            {
                throw new InvalidOperationException("KNX bus is not connected. Call StartListening first.");
            }

            try
            {
                var parsedAddress = GroupAddress.Parse(groupAddress);

                // Create GroupValue based on the value type
                GroupValue groupValue;
                if (value is bool boolValue)
                {
                    groupValue = new GroupValue(boolValue);
                    Console.WriteLine($"[KnxService] Writing bool to {groupAddress}: {boolValue}");
                }
                else if (value is byte[] byteArray)
                {
                    groupValue = new GroupValue(byteArray);
                    Console.WriteLine($"[KnxService] Writing bytes to {groupAddress}: {BitConverter.ToString(byteArray)}");
                }
                else if (value is ValueTuple<byte, int> fourBitValue)
                {
                    // For 4-bit values like DPST-3-7, use the special constructor with sizeInBit parameter
                    groupValue = new GroupValue(fourBitValue.Item1, fourBitValue.Item2);
                    Console.WriteLine($"[KnxService] Writing {fourBitValue.Item2}-bit value to {groupAddress}: {fourBitValue.Item1} (0x{fourBitValue.Item1:X2})");
                }
                else if (value is byte byteValue)
                {
                    // Regular 8-bit byte value
                    groupValue = new GroupValue(byteValue);
                    Console.WriteLine($"[KnxService] Writing 8-bit byte to {groupAddress}: {byteValue} (0x{byteValue:X2})");
                }
                else
                {
                    throw new ArgumentException($"Unsupported value type: {value.GetType().Name}");
                }

                Console.WriteLine($"[KnxService] GroupValue.Value property: {BitConverter.ToString(groupValue.Value)}");
                Console.WriteLine($"[KnxService] GroupValue.Value length: {groupValue.Value.Length}");

                var result = await _bus.WriteGroupValueAsync(parsedAddress, groupValue, cancellationToken: cancellationToken);

                Console.WriteLine($"[KnxService] Write completed successfully: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KnxService] Write failed with exception: {ex.GetType().Name}");
                Console.WriteLine($"[KnxService] Error message: {ex.Message}");
                Console.WriteLine($"[KnxService] Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}
