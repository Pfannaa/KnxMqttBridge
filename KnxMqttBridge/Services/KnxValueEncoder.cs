using System.Text.Json;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Logging;

namespace KnxMqttBridge.Services
{
    public class KnxValueEncoder : IKnxValueEncoder
    {
        private readonly ILogger<KnxValueEncoder> _logger;

        public KnxValueEncoder(ILogger<KnxValueEncoder> logger)
        {
            _logger = logger;
        }

        public object EncodeValue(string value, string dataPointType)
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
                    // Return as tuple: (byte value, bit size) for 4-bit encoding
                    "DPST-3-7" => (EncodeDimmingControl(value), 4),

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

                // DPST-3-7 encoding:
                // Bit 3 (0x08): Control bit - 1=increase/up, 0=decrease/down
                // Bits 0-2: Steps (0-7)
                if (isIncrease) // up = 1
                    control |= 0x08;

                control |= (byte)steps;

                _logger.LogInformation("Encoded dimming control: Direction={Direction}, Steps={Steps}, ControlBit={ControlBit}, Byte=0x{Byte:X2}",
                    dim.Direction, dim.Steps, isIncrease ? 1 : 0, control);

                return control;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encoding dimming control from JSON: {Json}", json);
                return 0;
            }
        }
    }
}
