using Knx.Falcon;
using Knx.Falcon.Configuration;
using Knx.Falcon.Discovery;
using Knx.Falcon.KnxnetIp;
using Knx.Falcon.Sdk;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace KnxMqttBridge.Services
{
    internal class KnxService : IKnxService, IDisposable
    {
        public event EventHandler<GroupEventArgs> GroupMessageReceived;

        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;
        private readonly IOptions<KnxConfiguration> _knxConfiguration;
        private readonly ILogger<KnxService> _logger;
        private KnxBus? _bus;
        private bool _disposed;
        private EventHandler<GroupEventArgs>? _busEventHandler;

        public KnxService(
            IOptions<GroupAddressInformation> groupAddressInformation,
            IOptions<KnxConfiguration> knxConfiguration,
            ILogger<KnxService> logger)
        {
            _groupAddressInformation = groupAddressInformation;
            _knxConfiguration = knxConfiguration;
            _logger = logger;
        }

        public async Task StartListening(CancellationToken cancellationToken)
        {
            try
            {
                ConnectorParameters connectorParameters;
                var config = _knxConfiguration.Value;

                // Check if manual configuration is provided
                if (!config.UseAutoDiscovery && !string.IsNullOrEmpty(config.GatewayIp))
                {
                    // Use manual configuration - build connection string matching auto-discovery format
                    var connectionString = $"Type=IpTunneling;HostAddress={config.GatewayIp};ProtocolType=Tcp;UseNat=True";
                    _logger.LogInformation("Using manual KNX gateway configuration: {ConnectionString}", connectionString);

                    connectorParameters = ConnectorParameters.FromConnectionString(connectionString);
                }
                else
                {
                    // Use auto-discovery
                    _logger.LogInformation("Using auto-discovery to find KNX/IP gateway...");

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
                        var errorMessage = "Failed to find KNX/IP gateway via auto-discovery. " +
                                         "Please ensure a KNX/IP gateway is available on the network, " +
                                         "or configure manual connection using GatewayIp in appsettings.json.";
                        _logger.LogError(errorMessage);
                        throw new InvalidOperationException(errorMessage);
                    }

                    _logger.LogInformation("Discovered KNX gateway: {ConnectionString}", connectionStrings[0]);
                    connectorParameters = ConnectorParameters.FromConnectionString(connectionStrings[0]);
                }

                _bus = new KnxBus(connectorParameters);
                await _bus.ConnectAsync(cancellationToken);

                _logger.LogInformation("Successfully connected to KNX bus");

                _busEventHandler = (sender, args) =>
                {
                    _logger.LogDebug("Received KNX telegram - Destination: {Destination}, Source: {Source}, Type: {Type}",
                        args.DestinationAddress, args.SourceAddress, args.EventType);
                    GroupMessageReceived?.Invoke(this, args);
                };
                _bus.GroupMessageReceived += _busEventHandler;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start KNX service and connect to gateway");
                throw;
            }
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
                    _logger.LogDebug("Writing bool to {Address}: {Value}", groupAddress, boolValue);
                }
                else if (value is byte[] byteArray)
                {
                    groupValue = new GroupValue(byteArray);
                    _logger.LogDebug("Writing bytes to {Address}: {Value}", groupAddress, BitConverter.ToString(byteArray));
                }
                else if (value is ValueTuple<byte, int> fourBitValue)
                {
                    // For 4-bit values like DPST-3-7, use the special constructor with sizeInBit parameter
                    groupValue = new GroupValue(fourBitValue.Item1, fourBitValue.Item2);
                    _logger.LogDebug("Writing {Bits}-bit value to {Address}: {Value} (0x{ValueHex:X2})",
                        fourBitValue.Item2, groupAddress, fourBitValue.Item1, fourBitValue.Item1);
                }
                else if (value is byte byteValue)
                {
                    // Regular 8-bit byte value
                    groupValue = new GroupValue(byteValue);
                    _logger.LogDebug("Writing 8-bit byte to {Address}: {Value} (0x{ValueHex:X2})",
                        groupAddress, byteValue, byteValue);
                }
                else
                {
                    throw new ArgumentException($"Unsupported value type: {value.GetType().Name}");
                }

                _logger.LogDebug("GroupValue bytes: {Bytes}, Length: {Length}",
                    BitConverter.ToString(groupValue.Value), groupValue.Value.Length);

                var result = await _bus.WriteGroupValueAsync(parsedAddress, groupValue, cancellationToken: cancellationToken);

                _logger.LogDebug("KNX write completed with result: {Result}", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write value to KNX address {Address}", groupAddress);
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _bus?.Dispose();
        }
    }
}
