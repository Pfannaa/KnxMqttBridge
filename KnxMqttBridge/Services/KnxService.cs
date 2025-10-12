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
                else if (value is byte byteValue)
                {
                    groupValue = new GroupValue(new[] { byteValue });
                    Console.WriteLine($"[KnxService] Writing byte to {groupAddress}: {byteValue}");
                }
                else
                {
                    throw new ArgumentException($"Unsupported value type: {value.GetType().Name}");
                }

                var result = await _bus.WriteGroupValueAsync(parsedAddress, groupValue, cancellationToken: cancellationToken);

                Console.WriteLine($"[KnxService] Write completed successfully: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KnxService] Write failed: {ex.Message}");
                throw;
            }
        }
    }
}
