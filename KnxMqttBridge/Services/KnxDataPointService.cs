using KnxMqttBridge.Models;
using KnxMqttBridge.Services.Abstractions;
using System.Text;
using System.Text.Json;

namespace KnxMqttBridge.Services
{
    public class KnxDataPointService : IKnxDataPointService
    {
        private readonly ILogger<KnxDataPointService> _logger;

        public KnxDataPointService(ILogger<KnxDataPointService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Decode raw KNX value based on DPT or heuristics
        /// </summary>
        public object DecodeValue(byte[] rawValue, string? dataPointType = null)
        {
            if (rawValue == null || rawValue.Length == 0)
            {
                return null;
            }

            // If DPT is known, use it for accurate decoding
            if (!string.IsNullOrEmpty(dataPointType))
            {
                return DecodeByDataPointType(rawValue, dataPointType);
            }

            // Heuristic-based decoding when DPT is unknown
            return DecodeHeuristic(rawValue);
        }

        /// <summary>
        /// Encode value based on DPT
        /// </summary>
        public object EncodeValue(object value, string dataPointType)
        {
            try
            {
                // DPT 1.x - Boolean
                if (dataPointType.StartsWith("DPST-1-") || dataPointType.StartsWith("DPT-1-") || dataPointType == "DPT-1")
                {
                    return EncodeBoolean(value);
                }
                // DPT 2.x - 1-bit controlled
                else if (dataPointType.StartsWith("DPST-2-") || dataPointType.StartsWith("DPT-2"))
                {
                    return EncodeBitControlled(value);
                }
                // DPT 3.x - 3-bit controlled (dimming, blinds)
                else if (dataPointType.StartsWith("DPST-3-") || dataPointType.StartsWith("DPT-3"))
                {
                    return Encode3BitControlled(value);
                }
                // DPT 4.x - Character
                else if (dataPointType.StartsWith("DPST-4-") || dataPointType.StartsWith("DPT-4"))
                {
                    return EncodeCharacter(value);
                }
                // DPT 5.x - 8-bit unsigned value
                else if (dataPointType.StartsWith("DPST-5-") || dataPointType.StartsWith("DPT-5"))
                {
                    return EncodeUInt8(value, dataPointType);
                }
                // DPT 6.x - 8-bit signed value
                else if (dataPointType.StartsWith("DPST-6-") || dataPointType.StartsWith("DPT-6"))
                {
                    return EncodeInt8(value);
                }
                // DPT 7.x - 16-bit unsigned value
                else if (dataPointType.StartsWith("DPST-7-") || dataPointType.StartsWith("DPT-7"))
                {
                    return EncodeUInt16(value);
                }
                // DPT 8.x - 16-bit signed value
                else if (dataPointType.StartsWith("DPST-8-") || dataPointType.StartsWith("DPT-8"))
                {
                    return EncodeInt16(value);
                }
                // DPT 9.x - 16-bit float
                else if (dataPointType.StartsWith("DPST-9-") || dataPointType.StartsWith("DPT-9"))
                {
                    return EncodeFloat16(value);
                }
                // DPT 10.x - Time
                else if (dataPointType.StartsWith("DPST-10-") || dataPointType.StartsWith("DPT-10"))
                {
                    return EncodeTime(value);
                }
                // DPT 11.x - Date
                else if (dataPointType.StartsWith("DPST-11-") || dataPointType.StartsWith("DPT-11"))
                {
                    return EncodeDate(value);
                }
                // DPT 12.x - 32-bit unsigned value
                else if (dataPointType.StartsWith("DPST-12-") || dataPointType.StartsWith("DPT-12"))
                {
                    return EncodeUInt32(value);
                }
                // DPT 13.x - 32-bit signed value
                else if (dataPointType.StartsWith("DPST-13-") || dataPointType.StartsWith("DPT-13"))
                {
                    return EncodeInt32(value);
                }
                // DPT 14.x - 32-bit float
                else if (dataPointType.StartsWith("DPST-14-") || dataPointType.StartsWith("DPT-14"))
                {
                    return EncodeFloat32(value);
                }
                // DPT 15.x - Entrance access
                else if (dataPointType.StartsWith("DPST-15-") || dataPointType.StartsWith("DPT-15"))
                {
                    return EncodeEntranceAccess(value);
                }
                // DPT 16.x - Character string
                else if (dataPointType.StartsWith("DPST-16-") || dataPointType.StartsWith("DPT-16"))
                {
                    return EncodeString(value);
                }
                // DPT 17.x - Scene number
                else if (dataPointType.StartsWith("DPST-17-") || dataPointType.StartsWith("DPT-17"))
                {
                    return EncodeSceneNumber(value);
                }
                // DPT 18.x - Scene control
                else if (dataPointType.StartsWith("DPST-18-") || dataPointType.StartsWith("DPT-18"))
                {
                    return EncodeSceneControl(value);
                }
                // DPT 19.x - Date/Time
                else if (dataPointType.StartsWith("DPST-19-") || dataPointType.StartsWith("DPT-19"))
                {
                    return EncodeDateTime(value);
                }
                else
                {
                    throw new NotSupportedException($"Data point type '{dataPointType}' is not supported");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encode value '{Value}' for DPT {DataPointType}", value, dataPointType);
                throw;
            }
        }

        /// <summary>
        /// Get the classic programming data type for a DPT
        /// </summary>
        public string GetClassicDataType(string dataPointType)
        {
            var dptEnum = KnxDataPointTypeExtensions.ParseDataPointType(dataPointType);
            return dptEnum?.GetClassicDataType() ?? "object";
        }

        /// <summary>
        /// Get human-readable description of a DPT
        /// </summary>
        public string GetDataPointDescription(string dataPointType)
        {
            var dptEnum = KnxDataPointTypeExtensions.ParseDataPointType(dataPointType);
            return dptEnum?.GetDescription() ?? dataPointType;
        }

        #region Decoding Methods

        private object DecodeByDataPointType(byte[] rawValue, string dataPointType)
        {
            try
            {
                // DPT 1.x - Boolean
                if (dataPointType.StartsWith("DPST-1-") || dataPointType.StartsWith("DPT-1-") || dataPointType == "DPT-1")
                {
                    return DecodeBoolean(rawValue);
                }
                // DPT 2.x - 1-bit controlled
                else if (dataPointType.StartsWith("DPST-2-") || dataPointType.StartsWith("DPT-2"))
                {
                    return DecodeBitControlled(rawValue);
                }
                // DPT 3.x - 3-bit controlled
                else if (dataPointType.StartsWith("DPST-3-") || dataPointType.StartsWith("DPT-3"))
                {
                    return Decode3BitControlled(rawValue);
                }
                // DPT 4.x - Character
                else if (dataPointType.StartsWith("DPST-4-") || dataPointType.StartsWith("DPT-4"))
                {
                    return DecodeCharacter(rawValue);
                }
                // DPT 5.x - 8-bit unsigned
                else if (dataPointType.StartsWith("DPST-5-") || dataPointType.StartsWith("DPT-5"))
                {
                    return DecodeUInt8(rawValue, dataPointType);
                }
                // DPT 6.x - 8-bit signed
                else if (dataPointType.StartsWith("DPST-6-") || dataPointType.StartsWith("DPT-6"))
                {
                    return DecodeInt8(rawValue);
                }
                // DPT 7.x - 16-bit unsigned
                else if (dataPointType.StartsWith("DPST-7-") || dataPointType.StartsWith("DPT-7"))
                {
                    return DecodeUInt16(rawValue);
                }
                // DPT 8.x - 16-bit signed
                else if (dataPointType.StartsWith("DPST-8-") || dataPointType.StartsWith("DPT-8"))
                {
                    return DecodeInt16(rawValue);
                }
                // DPT 9.x - 16-bit float
                else if (dataPointType.StartsWith("DPST-9-") || dataPointType.StartsWith("DPT-9"))
                {
                    return DecodeFloat16(rawValue);
                }
                // DPT 10.x - Time
                else if (dataPointType.StartsWith("DPST-10-") || dataPointType.StartsWith("DPT-10"))
                {
                    return DecodeTime(rawValue);
                }
                // DPT 11.x - Date
                else if (dataPointType.StartsWith("DPST-11-") || dataPointType.StartsWith("DPT-11"))
                {
                    return DecodeDate(rawValue);
                }
                // DPT 12.x - 32-bit unsigned
                else if (dataPointType.StartsWith("DPST-12-") || dataPointType.StartsWith("DPT-12"))
                {
                    return DecodeUInt32(rawValue);
                }
                // DPT 13.x - 32-bit signed
                else if (dataPointType.StartsWith("DPST-13-") || dataPointType.StartsWith("DPT-13"))
                {
                    return DecodeInt32(rawValue);
                }
                // DPT 14.x - 32-bit float
                else if (dataPointType.StartsWith("DPST-14-") || dataPointType.StartsWith("DPT-14"))
                {
                    return DecodeFloat32(rawValue);
                }
                // DPT 15.x - Entrance access
                else if (dataPointType.StartsWith("DPST-15-") || dataPointType.StartsWith("DPT-15"))
                {
                    return DecodeEntranceAccess(rawValue);
                }
                // DPT 16.x - String
                else if (dataPointType.StartsWith("DPST-16-") || dataPointType.StartsWith("DPT-16"))
                {
                    return DecodeString(rawValue);
                }
                // DPT 17.x - Scene number
                else if (dataPointType.StartsWith("DPST-17-") || dataPointType.StartsWith("DPT-17"))
                {
                    return DecodeSceneNumber(rawValue);
                }
                // DPT 18.x - Scene control
                else if (dataPointType.StartsWith("DPST-18-") || dataPointType.StartsWith("DPT-18"))
                {
                    return DecodeSceneControl(rawValue);
                }
                // DPT 19.x - DateTime
                else if (dataPointType.StartsWith("DPST-19-") || dataPointType.StartsWith("DPT-19"))
                {
                    return DecodeDateTime(rawValue);
                }
                else
                {
                    return Convert.ToBase64String(rawValue);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode value for DPT {DataPointType}, raw: {RawValue}",
                    dataPointType, BitConverter.ToString(rawValue));
                return Convert.ToBase64String(rawValue);
            }
        }

        private object DecodeHeuristic(byte[] rawValue)
        {
            // Heuristic-based decoding when DPT is unknown
            switch (rawValue.Length)
            {
                case 1:
                    // Could be boolean (0x00/0x01) or 8-bit value
                    if (rawValue[0] == 0x00 || rawValue[0] == 0x01)
                    {
                        return (int)rawValue[0]; // Return as int (0 or 1) for time-series database compatibility
                    }
                    return rawValue[0]; // Return as byte

                case 2:
                    // Likely a 16-bit float or 16-bit integer
                    try
                    {
                        return DecodeFloat16(rawValue);
                    }
                    catch
                    {
                        return BitConverter.ToUInt16(rawValue, 0);
                    }

                case 3:
                    // Could be time or date
                    return Convert.ToBase64String(rawValue);

                case 4:
                    // Could be 32-bit float or integer
                    try
                    {
                        return DecodeFloat32(rawValue);
                    }
                    catch
                    {
                        return BitConverter.ToUInt32(rawValue, 0);
                    }

                default:
                    // For longer values, return as base64
                    return Convert.ToBase64String(rawValue);
            }
        }

        // DPT 1.x - Boolean
        // Returns int (0 or 1) instead of bool for better compatibility with time-series databases
        private int DecodeBoolean(byte[] data)
        {
            return (data.Length > 0 && (data[0] & 0x01) != 0) ? 1 : 0;
        }

        // DPT 2.x - 1-bit controlled (value + control)
        private object DecodeBitControlled(byte[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }
            return new { Control = (data[0] & 0x02) != 0, Value = (data[0] & 0x01) != 0 };
        }

        // DPT 3.x - 3-bit controlled (dimming, blinds)
        private object Decode3BitControlled(byte[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }
            bool control = (data[0] & 0x08) != 0;
            int steps = data[0] & 0x07;
            return new { Control = control, Steps = steps };
        }

        // DPT 4.x - Character
        private char DecodeCharacter(byte[] data)
        {
            return data.Length > 0 ? (char)data[0] : '\0';
        }

        // DPT 5.x - 8-bit unsigned
        private object DecodeUInt8(byte[] data, string dataPointType)
        {
            if (data.Length == 0)
            {
                return 0;
            }

            // DPT 5.1 is percentage (0-100%)
            if (dataPointType == "DPST-5-1" || dataPointType == "DPT-5.001")
            {
                return Math.Round(data[0] * 100.0 / 255.0, 1);
            }

            return data[0];
        }

        // DPT 6.x - 8-bit signed
        private sbyte DecodeInt8(byte[] data)
        {
            return data.Length > 0 ? (sbyte)data[0] : (sbyte)0;
        }

        // DPT 7.x - 16-bit unsigned
        private ushort DecodeUInt16(byte[] data)
        {
            if (data.Length < 2)
            {
                return 0;
            }
            return (ushort)((data[0] << 8) | data[1]);
        }

        // DPT 8.x - 16-bit signed
        private short DecodeInt16(byte[] data)
        {
            if (data.Length < 2)
            {
                return 0;
            }
            return (short)((data[0] << 8) | data[1]);
        }

        // DPT 9.x - 16-bit float
        private float DecodeFloat16(byte[] data)
        {
            if (data.Length < 2)
            {
                return 0;
            }

            int value = (data[0] << 8) | data[1];
            int sign = (value & 0x8000) >> 15;
            int exponent = (value & 0x7800) >> 11;
            int mantissa = value & 0x07FF;

            float result = (1 << exponent) * (mantissa / 100.0f);
            return sign == 1 ? -result : result;
        }

        // DPT 10.x - Time
        private string DecodeTime(byte[] data)
        {
            if (data.Length < 3)
            {
                return null;
            }
            int day = (data[0] & 0xE0) >> 5; // Day of week (0-7)
            int hour = data[0] & 0x1F;
            int minute = data[1] & 0x3F;
            int second = data[2] & 0x3F;
            return $"{hour:D2}:{minute:D2}:{second:D2}";
        }

        // DPT 11.x - Date
        private string DecodeDate(byte[] data)
        {
            if (data.Length < 3)
            {
                return null;
            }
            int day = data[0] & 0x1F;
            int month = data[1] & 0x0F;
            int rawYear = data[2] & 0x7F;
            int year = rawYear >= 90 ? 1900 + rawYear : 2000 + rawYear;
            return $"{year:D4}-{month:D2}-{day:D2}";
        }

        // DPT 12.x - 32-bit unsigned
        private uint DecodeUInt32(byte[] data)
        {
            if (data.Length < 4)
            {
                return 0;
            }
            return (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
        }

        // DPT 13.x - 32-bit signed
        private int DecodeInt32(byte[] data)
        {
            if (data.Length < 4)
            {
                return 0;
            }
            return (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];
        }

        // DPT 14.x - 32-bit float
        private float DecodeFloat32(byte[] data)
        {
            if (data.Length < 4)
            {
                return 0;
            }
            var bytes = data[..4];
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return BitConverter.ToSingle(bytes, 0);
        }

        // DPT 15.x - Entrance access
        private object DecodeEntranceAccess(byte[] data)
        {
            if (data.Length < 4)
            {
                return null;
            }
            return new
            {
                AccessCode = (data[0] << 16) | (data[1] << 8) | data[2],
                Error = (data[3] & 0x80) != 0,
                Permission = (data[3] & 0x40) != 0,
                Direction = (data[3] & 0x20) != 0,
                Encrypted = (data[3] & 0x10) != 0,
                Index = data[3] & 0x0F
            };
        }

        // DPT 16.x - String
        private string DecodeString(byte[] data)
        {
            return Encoding.ASCII.GetString(data).TrimEnd('\0');
        }

        // DPT 17.x - Scene number
        private byte DecodeSceneNumber(byte[] data)
        {
            return data.Length > 0 ? (byte)(data[0] & 0x3F) : (byte)0;
        }

        // DPT 18.x - Scene control
        private object DecodeSceneControl(byte[] data)
        {
            if (data.Length == 0)
            {
                return null;
            }
            return new
            {
                Learn = (data[0] & 0x80) != 0,
                SceneNumber = data[0] & 0x3F
            };
        }

        // DPT 19.x - DateTime
        private DateTime DecodeDateTime(byte[] data)
        {
            if (data.Length < 8)
            {
                return DateTime.MinValue;
            }

            int year = data[0];
            int month = data[1] & 0x0F;
            int day = data[2] & 0x1F;
            int hour = data[4] & 0x1F;
            int minute = data[5] & 0x3F;
            int second = data[6] & 0x3F;

            try
            {
                return new DateTime(year + 1900, month, day, hour, minute, second);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        #endregion

        #region Encoding Methods

        // DPT 1.x - Boolean
        private bool EncodeBoolean(object value)
        {
            if (value is bool b)
            {
                return b;
            }
            if (value is string s)
            {
                return s == "1" || s.ToLower() == "true" || s.ToLower() == "on";
            }
            if (value is int i)
            {
                return i > 0;
            }
            return Convert.ToBoolean(value);
        }

        // DPT 2.x - 1-bit controlled
        private byte EncodeBitControlled(object value)
        {
            // Expected format: { "Control": true/false, "Value": true/false }
            // Or just a simple value
            byte result = 0;
            // Implementation depends on input format
            return result;
        }

        // DPT 3.x - 3-bit controlled
        private (byte, int) Encode3BitControlled(object value)
        {
            // Expected format: { "Direction": "up"/"down", "Steps": 0-7 }
            // This returns a tuple for 4-bit encoding
            byte encoded = 0;

            if (value is string json)
            {
                var cmd = JsonSerializer.Deserialize<DimCommand>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cmd != null)
                {
                    bool isUp = string.Equals(cmd.Direction, "up", StringComparison.OrdinalIgnoreCase)
                             || string.Equals(cmd.Direction, "increase", StringComparison.OrdinalIgnoreCase);
                    if (isUp)
                    {
                        encoded |= 0x08;
                    }
                    encoded |= (byte)(cmd.Steps & 0x07);
                }
            }

            return (encoded, 4); // 4-bit value
        }

        // DPT 4.x - Character
        private byte EncodeCharacter(object value)
        {
            if (value is char c)
            {
                return (byte)c;
            }
            if (value is string s && s.Length > 0)
            {
                return (byte)s[0];
            }
            return 0;
        }

        // DPT 5.x - 8-bit unsigned
        private byte EncodeUInt8(object value, string dataPointType)
        {
            // DPT 5.1 is percentage (0-100%), convert to 0-255
            if (dataPointType == "DPST-5-1" || dataPointType == "DPT-5.001")
            {
                double percentage;
                if (value is double d)
                {
                    percentage = d;
                }
                else if (value is float f)
                {
                    percentage = f;
                }
                else if (value is int intVal)
                {
                    percentage = intVal;
                }
                else if (value is string strVal && double.TryParse(strVal, out double parsed))
                {
                    percentage = parsed;
                }
                else
                {
                    percentage = Convert.ToDouble(value);
                }

                // Clamp to 0-100% and convert to 0-255
                percentage = Math.Clamp(percentage, 0, 100);
                return (byte)Math.Round(percentage * 255.0 / 100.0);
            }

            // For other DPT 5.x types, treat as raw 0-255 value
            if (value is byte b)
            {
                return b;
            }
            if (value is int i)
            {
                return (byte)Math.Clamp(i, 0, 255);
            }
            if (value is string s && byte.TryParse(s, out byte result))
            {
                return result;
            }
            return Convert.ToByte(value);
        }

        // DPT 6.x - 8-bit signed
        private byte EncodeInt8(object value)
        {
            sbyte signed = value is sbyte sb ? sb : Convert.ToSByte(value);
            return (byte)signed;
        }

        // DPT 7.x - 16-bit unsigned
        private byte[] EncodeUInt16(object value)
        {
            ushort val = value is ushort us ? us : Convert.ToUInt16(value);
            return new byte[] { (byte)(val >> 8), (byte)(val & 0xFF) };
        }

        // DPT 8.x - 16-bit signed
        private byte[] EncodeInt16(object value)
        {
            short val = value is short s ? s : Convert.ToInt16(value);
            ushort unsigned = (ushort)val;
            return new byte[] { (byte)(unsigned >> 8), (byte)(unsigned & 0xFF) };
        }

        // DPT 9.x - 16-bit float
        private byte[] EncodeFloat16(object value)
        {
            float floatValue = value is float f ? f : Convert.ToSingle(value);

            int sign = floatValue < 0 ? 1 : 0;
            float absValue = Math.Abs(floatValue);

            int exponent = 0;
            while (absValue >= 20.48f && exponent < 15)
            {
                absValue /= 2;
                exponent++;
            }

            int mantissa = (int)(absValue * 100);
            mantissa = Math.Min(mantissa, 2047); // 11-bit max

            int encoded = (sign << 15) | (exponent << 11) | mantissa;
            return new byte[] { (byte)(encoded >> 8), (byte)(encoded & 0xFF) };
        }

        // DPT 10.x - Time
        private byte[] EncodeTime(object value)
        {
            TimeSpan time;
            if (value is TimeSpan ts)
            {
                time = ts;
            }
            else if (value is DateTime dt)
            {
                time = dt.TimeOfDay;
            }
            else if (value is string s && TimeSpan.TryParse(s, out var parsed))
            {
                time = parsed;
            }
            else
            {
                return new byte[3];
            }

            byte day = 0; // Day of week (0 = no day)
            byte hour = (byte)time.Hours;
            byte minute = (byte)time.Minutes;
            byte second = (byte)time.Seconds;

            return new byte[]
            {
                (byte)((day << 5) | (hour & 0x1F)),
                (byte)(minute & 0x3F),
                (byte)(second & 0x3F)
            };
        }

        // DPT 11.x - Date
        private byte[] EncodeDate(object value)
        {
            DateTime date = value is DateTime dt ? dt : Convert.ToDateTime(value);

            if (date.Year < 1990 || date.Year > 2089)
                throw new ArgumentOutOfRangeException(nameof(value), $"Year {date.Year} is outside the KNX DPT 11.001 supported range (1990-2089)");

            byte day = (byte)date.Day;
            byte month = (byte)date.Month;
            int y = date.Year;
            byte year = y >= 2000 ? (byte)(y - 2000) : (byte)(y - 1900);

            return new byte[] { day, month, year };
        }

        // DPT 12.x - 32-bit unsigned
        private byte[] EncodeUInt32(object value)
        {
            uint val = value is uint ui ? ui : Convert.ToUInt32(value);
            return new byte[]
            {
                (byte)(val >> 24),
                (byte)((val >> 16) & 0xFF),
                (byte)((val >> 8) & 0xFF),
                (byte)(val & 0xFF)
            };
        }

        // DPT 13.x - 32-bit signed
        private byte[] EncodeInt32(object value)
        {
            int val = value is int i ? i : Convert.ToInt32(value);
            uint unsigned = (uint)val;
            return new byte[]
            {
                (byte)(unsigned >> 24),
                (byte)((unsigned >> 16) & 0xFF),
                (byte)((unsigned >> 8) & 0xFF),
                (byte)(unsigned & 0xFF)
            };
        }

        // DPT 14.x - 32-bit float
        private byte[] EncodeFloat32(object value)
        {
            float val = value is float f ? f : Convert.ToSingle(value);
            byte[] bytes = BitConverter.GetBytes(val);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        // DPT 15.x - Entrance access
        private byte[] EncodeEntranceAccess(object value)
        {
            // Complex encoding - simplified version
            return new byte[4];
        }

        // DPT 16.x - String
        private byte[] EncodeString(object value)
        {
            string str = value?.ToString() ?? string.Empty;
            byte[] bytes = new byte[14]; // Max 14 characters for KNX
            byte[] encoded = Encoding.ASCII.GetBytes(str);
            Array.Copy(encoded, bytes, Math.Min(encoded.Length, 14));
            return bytes;
        }

        // DPT 17.x - Scene number
        private byte EncodeSceneNumber(object value)
        {
            byte scene = value is byte b ? b : Convert.ToByte(value);
            return (byte)(scene & 0x3F); // Max 63
        }

        // DPT 18.x - Scene control
        private byte EncodeSceneControl(object value)
        {
            // Expected format: { "Learn": true/false, "SceneNumber": 0-63 }
            if (value is string json)
            {
                var cmd = JsonSerializer.Deserialize<SceneControlCommand>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (cmd != null)
                {
                    return (byte)((cmd.Learn ? 0x80 : 0) | (cmd.SceneNumber & 0x3F));
                }
            }

            // Backward compat: plain integer/byte
            byte scene = value is byte b ? b : Convert.ToByte(value);
            return (byte)(scene & 0x3F);
        }

        private class SceneControlCommand
        {
            public bool Learn { get; set; }
            public int SceneNumber { get; set; }
        }

        // DPT 19.x - DateTime
        private byte[] EncodeDateTime(object value)
        {
            DateTime dt = value is DateTime dateTime ? dateTime : Convert.ToDateTime(value);

            return new byte[]
            {
                (byte)(dt.Year - 1900),
                (byte)dt.Month,
                (byte)dt.Day,
                0, // Day of week
                (byte)dt.Hour,
                (byte)dt.Minute,
                (byte)dt.Second,
                0  // Flags
            };
        }

        #endregion
    }
}
