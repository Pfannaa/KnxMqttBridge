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
        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;


        public Worker(ILogger<Worker> logger, IKnxService knxService, IMqttService mqttService, IOptions<GroupAddressInformation> groupAddressInformation)
        {
            _logger = logger;
            _knxService = knxService;
            _mqttService = mqttService;
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
                var encodedValue = EncodeValue(payload, groupAddressInfo.DataPointType);
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

        private object EncodeValue(string value, string dataPointType)
        {
            try
            {
                return dataPointType switch
                {
                    // Boolean types (switch on/off) - return bool directly
                    "DPST-1-1" or "DPST-1-11" or "DPST-1-24" => byte.Parse(value) > 0,

                    // Unsigned 8-bit (brightness 0-255) - return byte
                    "DPST-5-1" or "DPST-5-10" => byte.Parse(value),

                    // 2-byte float (temperature) - encode as byte array
                    "DPST-9-1" or "DPST-9-4" or "DPST-9-5" => EncodeFloat16(float.Parse(value)),

                    // Dimming control (expects JSON like {"Direction": "up", "Steps": 5})
                    "DPST-3-7" => EncodeDimmingControl(value),

                    // Scene number
                    "DPST-18-1" => byte.Parse(value),

                    _ => null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encoding value '{Value}' for DPT {DataPointType}", value, dataPointType);
                return null;
            }
        }

        private byte[] EncodeFloat16(float value)
        {
            int sign = value < 0 ? 1 : 0;
            float absValue = Math.Abs(value);

            int exponent = 0;
            while (absValue >= 20.48f && exponent < 15)
            {
                absValue /= 2;
                exponent++;
            }

            int mantissa = (int)(absValue * 100);
            mantissa = Math.Min(mantissa, 2047); // 11-bit max

            int encoded = (sign << 15) | (exponent << 11) | mantissa;
            return new[] { (byte)(encoded >> 8), (byte)(encoded & 0xFF) };
        }

        private byte EncodeDimmingControl(string json)
        {
            try
            {
                var dim = JsonSerializer.Deserialize<DimCommand>(json);
                if (dim == null)
                {
                    _logger.LogWarning("Failed to deserialize dimming command JSON: {Json}", json);
                    return 0;
                }

                byte control = 0;
                bool isIncrease = dim.Direction?.ToLower() == "up";
                int steps = dim.Steps & 0x07;

                // DPST-3-7: bit 3 = control (1=decrease, 0=increase), bits 0-2 = steps
                // BUT the control bit logic is inverted from what you'd expect!
                if (!isIncrease) // down = 1
                    control |= 0x08;

                control |= (byte)steps;

                _logger.LogInformation("Encoded dimming control: Direction={Direction}, Steps={Steps}, ControlBit={ControlBit}, Byte=0x{Byte:X2}",
                    dim.Direction, dim.Steps, isIncrease ? 0 : 1, control);

                return control;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encoding dimming control from JSON: {Json}", json);
                return 0;
            }
        }

        private class DimCommand
        {
            public string Direction { get; set; }
            public int Steps { get; set; }
        }
    }
}
