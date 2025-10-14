using System.Text;
using System.Text.Json;
using Knx.Falcon;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace KnxMqttBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IKnxService _knxService;
        private readonly IMqttService _mqttService;
        private readonly IKnxValueEncoder _valueEncoder;
        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;

        public Worker(ILogger<Worker> logger, IKnxService knxService, IMqttService mqttService, IKnxValueEncoder valueEncoder, IOptions<GroupAddressInformation> groupAddressInformation)
        {
            _logger = logger;
            _knxService = knxService;
            _mqttService = mqttService;
            _valueEncoder = valueEncoder;
            _groupAddressInformation = groupAddressInformation;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _knxService.StartListening(cancellationToken);
            _knxService.GroupMessageReceived += KnxGroupMessageReceived;

            await _mqttService.ConnectAsync(cancellationToken);
            _mqttService.MessageReceived += MqttMessageReceived;

            // Subscribe to command topics: knx/command/+
            await _mqttService.SubscribeAsync("command/#", cancellationToken);

            _logger.LogInformation("KNX-MQTT Bridge is running. Listening for KNX events and MQTT commands.");
        }

        private async void KnxGroupMessageReceived(object? sender, GroupEventArgs e)
        {
            var groupAddressInfo = _groupAddressInformation.Value.GetByAddress(e.DestinationAddress);
            groupAddressInfo.RawValue = e.Value.Value;
            groupAddressInfo.Value = groupAddressInfo.GetDecodedValue();
            groupAddressInfo.LastUpdated = DateTime.Now;

            var payload = JsonSerializer.Serialize(groupAddressInfo);

            await _mqttService.PublishAsync($"{groupAddressInfo.Subcategory}/{groupAddressInfo.Category}/{groupAddressInfo.Name}", payload, true);
        }

        private async Task MqttMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                var topic = e.ApplicationMessage.Topic;
                var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                _logger.LogInformation("Received MQTT command on topic: {Topic}, Payload: {Payload}", topic, payload);

                // Parse command format: knx/command/{address-with-dashes}
                // Example: knx/command/2-1-71 for KNX address 2/1/71
                var parts = topic.Split('/');

                if (parts.Length < 3 || parts[1] != "command")
                {
                    _logger.LogWarning("Invalid command topic format: {Topic}. Expected knx/command/{{address-with-dashes}}", topic);
                    return;
                }

                // Convert dashes back to slashes for KNX address
                var addressWithDashes = parts[2];
                var address = addressWithDashes.Replace('-', '/');
                _logger.LogInformation("Extracted KNX address: {Address} (from {AddressWithDashes})", address, addressWithDashes);

                var groupAddressInfo = _groupAddressInformation.Value.GetByAddress(address);

                if (groupAddressInfo == null)
                {
                    _logger.LogWarning("Unknown group address in command: {Address}. Check your ETS export.", address);
                    return;
                }

                _logger.LogInformation("Found group address: {Name}, DPT: {DataPointType}", groupAddressInfo.Name, groupAddressInfo.DataPointType);

                // Encode the value based on data point type
                var encodedValue = _valueEncoder.EncodeValue(payload, groupAddressInfo.DataPointType);
                if (encodedValue == null)
                {
                    _logger.LogWarning("Failed to encode value '{Payload}' for DPT {DataPointType}", payload, groupAddressInfo.DataPointType);
                    return;
                }

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
