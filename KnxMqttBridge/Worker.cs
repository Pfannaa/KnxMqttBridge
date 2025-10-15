using Knx.Falcon;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Text;
using System.Text.Json;

namespace KnxMqttBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IKnxService _knxService;
        private readonly IMqttService _mqttService;
        private readonly IKnxDataPointService _dataPointService;
        private readonly IMqttTopicParser _mqttTopicParser;
        private readonly IKnxCommandHandler _knxCommandHandler;
        private readonly GroupAddressInformation? _groupAddressInformation;
        private readonly KnxConfiguration _knxConfiguration;

        public Worker(
            ILogger<Worker> logger,
            IKnxService knxService,
            IMqttService mqttService,
            IKnxDataPointService dataPointService,
            IMqttTopicParser mqttTopicParser,
            IKnxCommandHandler knxCommandHandler,
            IOptions<KnxConfiguration> knxConfiguration,
            IOptions<GroupAddressInformation>? groupAddressInformation = null)
        {
            _logger = logger;
            _knxService = knxService;
            _mqttService = mqttService;
            _dataPointService = dataPointService;
            _mqttTopicParser = mqttTopicParser;
            _knxCommandHandler = knxCommandHandler;
            _knxConfiguration = knxConfiguration.Value;
            _groupAddressInformation = groupAddressInformation?.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _knxService.StartListening(cancellationToken);
                _knxService.GroupMessageReceived += KnxGroupMessageReceived;

                await _mqttService.ConnectAsync(cancellationToken);
                _mqttService.MessageReceived += MqttMessageReceived;

                // Subscribe to command topics based on address style under GroupAddresses
                var commandTopic = _knxConfiguration.AddressStyle == KnxAddressStyle.TwoLevel
                    ? "GroupAddresses/+/+/command"      // knx/GroupAddresses/{main}/{sub}/command
                    : "GroupAddresses/+/+/+/command";   // knx/GroupAddresses/{main}/{middle}/{sub}/command

                await _mqttService.SubscribeAsync(commandTopic, cancellationToken);

                var configStatus = _groupAddressInformation != null
                    ? $"with {_groupAddressInformation.GroupAddresses.Count} configured addresses"
                    : "without ETS configuration (using heuristic decoding)";

                _logger.LogInformation("KNX-MQTT Bridge is running {ConfigStatus} in {AddressStyle} mode. Listening for KNX events and MQTT commands.",
                    configStatus, _knxConfiguration.AddressStyle);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to start KNX-MQTT Bridge. The application will terminate.");
                throw; // Re-throw to stop the application
            }
        }

        private async void KnxGroupMessageReceived(object? sender, GroupEventArgs e)
        {
            try
            {
                // Create KnxEvent with core data
                var destinationAddress = e.DestinationAddress.ToString();
                var knxEvent = new KnxEvent
                {
                    Address = destinationAddress,
                    RawValue = e.Value.Value,
                    Value = null!, // Will be set below
                    Timestamp = DateTime.UtcNow
                };

                // Try to get metadata from ETS export (if available)
                KnxGroupAddress? addressConfig = _groupAddressInformation?.GetByAddress(destinationAddress);

                if (addressConfig != null)
                {
                    // We have ETS configuration - use it for accurate decoding
                    knxEvent.Value = _dataPointService.DecodeValue(e.Value.Value, addressConfig.DataPointType);
                    knxEvent.Metadata = new KnxMetadata
                    {
                        Name = addressConfig.Name,
                        Category = addressConfig.Category,
                        Subcategory = addressConfig.Subcategory,
                        FullPath = addressConfig.FullPath,
                        DataPointType = addressConfig.DataPointType,
                        DataPointDescription = _dataPointService.GetDataPointDescription(addressConfig.DataPointType),
                        ClassicDataType = _dataPointService.GetClassicDataType(addressConfig.DataPointType),
                        Security = addressConfig.Security
                    };
                }
                else
                {
                    // No ETS configuration - use heuristic decoding
                    knxEvent.Value = _dataPointService.DecodeValue(e.Value.Value);

                    _logger.LogDebug("Received telegram on unconfigured address {Address}, using heuristic decoding",
                        destinationAddress);
                }

                // Publish to hierarchical topic under GroupAddresses:
                // ThreeLevel: knx/GroupAddresses/{main}/{middle}/{sub}/notification
                // TwoLevel: knx/GroupAddresses/{main}/{sub}/notification
                var topic = $"GroupAddresses/{destinationAddress}/notification";
                var payload = JsonSerializer.Serialize(knxEvent, new JsonSerializerOptions { WriteIndented = false });
                await _mqttService.PublishAsync(topic, payload, retain: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing KNX telegram for address {Address}", e.DestinationAddress.ToString());
            }
        }

        private async Task MqttMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                _logger.LogInformation("Received MQTT command on topic: {Topic}, Payload: {Payload}", topic, payload);

                // Parse KNX address from topic
                if (!_mqttTopicParser.TryParseAddressFromTopic(topic, out var address))
                    return;

                _logger.LogInformation("Extracted KNX address from topic: {Address}", address);

                // Deserialize command
                if (!_mqttTopicParser.TryDeserializeCommand(payload, out var command))
                    return;

                // Resolve data point type
                if (!_knxCommandHandler.TryResolveDataPointType(address, command, out var dataPointType))
                    return;

                // Encode value for KNX
                if (!_knxCommandHandler.TryEncodeValue(command.Value, dataPointType, out var encodedValue))
                    return;

                _logger.LogInformation("Encoded value: {EncodedValue} ({Type}) for KNX address {Address}",
                    encodedValue, encodedValue.GetType().Name, address);

                await _knxService.WriteAsync(address, encodedValue);

                _logger.LogInformation("Successfully sent command to KNX bus");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT command");
            }
        }
    }
}
