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

            KnxBus bus = new KnxBus(connectorParameters);
            await bus.ConnectAsync(cancellationToken);

            bus.GroupMessageReceived += (sender, args) =>
                GroupMessageReceived?.Invoke(this, args);
        }
    }
}
