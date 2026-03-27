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
        public event EventHandler<GroupEventArgs>? GroupMessageReceived;

        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;
        private readonly IOptions<KnxConfiguration> _knxConfiguration;
        private readonly ILogger<KnxService> _logger;
        private KnxBus? _bus;
        private ConnectorParameters? _connectorParameters;
        private readonly SemaphoreSlim _reconnectGuard = new(1, 1);
        private bool _disposed;

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
                _connectorParameters = await ResolveConnectorParametersAsync(cancellationToken);
                await ConnectBusAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start KNX service and connect to gateway");
                throw;
            }
        }

        private async Task<ConnectorParameters> ResolveConnectorParametersAsync(CancellationToken cancellationToken)
        {
            var config = _knxConfiguration.Value;

            if (!config.UseAutoDiscovery && !string.IsNullOrEmpty(config.GatewayIp))
            {
                var connectionString = $"Type=IpTunneling;HostAddress={config.GatewayIp};ProtocolType=Tcp;UseNat=True";
                _logger.LogInformation("Using manual KNX gateway configuration: {ConnectionString}", connectionString);
                return ConnectorParameters.FromConnectionString(connectionString);
            }

            _logger.LogInformation("Using auto-discovery to find KNX/IP gateway...");

            var ipDiscovery = new IpDeviceDiscovery { Timeout = TimeSpan.FromSeconds(1) };
            var ipDevices = await ipDiscovery.DiscoverAsync(cancellationToken)
                .Where(_ => _.Supports(ServiceFamily.Tunneling, 1), cancellationToken)
                .ToArray(CancellationToken.None);

            var connectionStrings = ipDevices
                .SelectMany(d => d.GetTunnelingConnections())
                .Select(t => t.ToConnectionString())
                .ToList();

            if (connectionStrings.Count < 1)
            {
                var errorMessage = "Failed to find KNX/IP gateway via auto-discovery. " +
                                   "Please ensure a KNX/IP gateway is available on the network, " +
                                   "or configure manual connection using GatewayIp in appsettings.json.";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            _logger.LogInformation("Discovered KNX gateway: {ConnectionString}", connectionStrings[0]);
            return ConnectorParameters.FromConnectionString(connectionStrings[0]);
        }

        private async Task ConnectBusAsync(CancellationToken cancellationToken)
        {
            _bus = new KnxBus(_connectorParameters!);
            await _bus.ConnectAsync(cancellationToken);

            _logger.LogInformation("Successfully connected to KNX bus");

            _bus.ConnectionStateChanged += OnConnectionStateChanged;
            _bus.GroupMessageReceived += OnGroupMessageReceived;
        }

        private void OnConnectionStateChanged(object? sender, EventArgs e)
        {
            // BusConnectionState.Broken means the SDK is already attempting to reestablish
            // the connection internally — no action needed from our side.
            // Only act on Closed, which means the SDK has given up.
            if (_bus?.ConnectionState != BusConnectionState.Closed)
                return;

            if (_disposed)
                return;

            _ = ReconnectAsync();
        }

        private async Task ReconnectAsync()
        {
            if (!_reconnectGuard.Wait(0))
                return;

            try
            {
                _logger.LogWarning("KNX connection closed. Attempting to reconnect...");

                while (!_disposed)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5));

                    if (_bus != null)
                    {
                        _bus.ConnectionStateChanged -= OnConnectionStateChanged;
                        _bus.GroupMessageReceived -= OnGroupMessageReceived;
                        _bus.Dispose();
                        _bus = null;
                    }

                    try
                    {
                        await ConnectBusAsync(CancellationToken.None);
                        _logger.LogInformation("Successfully reconnected to KNX bus");
                        return;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to reconnect to KNX bus. Retrying in 5 seconds...");
                    }
                }
            }
            finally
            {
                _reconnectGuard.Release();
            }
        }

        private void OnGroupMessageReceived(object? sender, GroupEventArgs args)
        {
            _logger.LogDebug("Received KNX telegram - Destination: {Destination}, Source: {Source}, Type: {Type}",
                args.DestinationAddress, args.SourceAddress, args.EventType);
            GroupMessageReceived?.Invoke(this, args);
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
                    groupValue = new GroupValue(fourBitValue.Item1, fourBitValue.Item2);
                    _logger.LogDebug("Writing {Bits}-bit value to {Address}: {Value} (0x{ValueHex:X2})",
                        fourBitValue.Item2, groupAddress, fourBitValue.Item1, fourBitValue.Item1);
                }
                else if (value is byte byteValue)
                {
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

            if (_bus != null)
            {
                _bus.ConnectionStateChanged -= OnConnectionStateChanged;
                _bus.GroupMessageReceived -= OnGroupMessageReceived;
                _bus.Dispose();
                _bus = null;
            }

            _reconnectGuard.Dispose();
        }
    }
}
