namespace KnxMqttBridge.Models
{
    /// <summary>
    /// Extension methods for KnxDataPointType enum
    /// </summary>
    public static class KnxDataPointTypeExtensions
    {
        /// <summary>
        /// Convert string DPT (e.g., "DPST-1-1") to enum
        /// </summary>
        public static KnxDataPointType? ParseDataPointType(string dptString)
        {
            if (string.IsNullOrWhiteSpace(dptString))
            {
                return null;
            }

            // Replace dashes with underscores for enum matching
            var enumString = dptString.Replace("-", "_");

            if (Enum.TryParse<KnxDataPointType>(enumString, true, out var result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Convert enum to string DPT (e.g., KnxDataPointType.DPST_1_1 -> "DPST-1-1")
        /// </summary>
        public static string ToDataPointString(this KnxDataPointType dpt)
        {
            return dpt.ToString().Replace("_", "-");
        }

        /// <summary>
        /// Get human-readable description for a data point type
        /// </summary>
        public static string GetDescription(this KnxDataPointType dpt)
        {
            return dpt switch
            {
                // Boolean types (DPT 1)
                KnxDataPointType.DPST_1_1 => "Switch",
                KnxDataPointType.DPST_1_2 => "Boolean",
                KnxDataPointType.DPST_1_3 => "Enable",
                KnxDataPointType.DPST_1_9 => "OpenClose",
                KnxDataPointType.DPST_1_10 => "Start",
                KnxDataPointType.DPST_1_11 => "State",
                KnxDataPointType.DPST_1_19 => "Window/Door",
                KnxDataPointType.DPST_1_22 => "DimSendStyle",
                KnxDataPointType.DPST_1_23 => "InputSource",
                KnxDataPointType.DPST_1_24 => "DayNight",

                // 4-bit control (DPT 3)
                KnxDataPointType.DPST_3_7 => "Dimming Control",
                KnxDataPointType.DPST_3_8 => "Blinds Control",

                // 8-bit unsigned (DPT 5)
                KnxDataPointType.DPST_5_1 => "Scaling (0-255)",
                KnxDataPointType.DPST_5_3 => "Angle (0-360°)",
                KnxDataPointType.DPST_5_4 => "Percentage (0-100%)",
                KnxDataPointType.DPST_5_5 => "Decimal Factor",
                KnxDataPointType.DPST_5_6 => "Tariff",
                KnxDataPointType.DPST_5_10 => "Counter Pulses (0-255)",

                // 8-bit signed (DPT 6)
                KnxDataPointType.DPST_6_1 => "Percentage (-128 to 127)",
                KnxDataPointType.DPST_6_10 => "Counter Pulses (-128 to 127)",
                KnxDataPointType.DPST_6_20 => "Status with Mode",

                // 16-bit unsigned (DPT 7)
                KnxDataPointType.DPST_7_1 => "Pulses (16-bit)",
                KnxDataPointType.DPST_7_2 => "Time Period (ms)",
                KnxDataPointType.DPST_7_3 => "Time Period (0.01s)",
                KnxDataPointType.DPST_7_4 => "Time Period (0.1s)",
                KnxDataPointType.DPST_7_5 => "Time Period (s)",
                KnxDataPointType.DPST_7_6 => "Time Period (min)",
                KnxDataPointType.DPST_7_7 => "Time Period (h)",
                KnxDataPointType.DPST_7_12 => "Current (mA)",
                KnxDataPointType.DPST_7_13 => "Brightness (Lux)",

                // 16-bit signed (DPT 8)
                KnxDataPointType.DPST_8_1 => "Pulses Signed (16-bit)",
                KnxDataPointType.DPST_8_2 => "Time Period Signed (ms)",
                KnxDataPointType.DPST_8_5 => "Time Period Signed (s)",
                KnxDataPointType.DPST_8_10 => "Percentage Signed",

                // 16-bit float (DPT 9)
                KnxDataPointType.DPST_9_1 => "Temperature (°C)",
                KnxDataPointType.DPST_9_4 => "Illuminance (Lux)",
                KnxDataPointType.DPST_9_5 => "Wind Speed (m/s)",
                KnxDataPointType.DPST_9_6 => "Pressure (Pa)",
                KnxDataPointType.DPST_9_7 => "Humidity (%)",
                KnxDataPointType.DPST_9_8 => "Air Quality (ppm)",
                KnxDataPointType.DPST_9_20 => "Voltage (mV)",
                KnxDataPointType.DPST_9_21 => "Current (mA)",
                KnxDataPointType.DPST_9_24 => "Power (kW)",

                // Time and Date (DPT 10, 11, 19)
                KnxDataPointType.DPST_10_1 => "Time of Day",
                KnxDataPointType.DPST_11_1 => "Date",
                KnxDataPointType.DPST_19_1 => "Date and Time",

                // 32-bit unsigned (DPT 12)
                KnxDataPointType.DPST_12_1 => "Counter Pulses (32-bit)",

                // 32-bit signed (DPT 13)
                KnxDataPointType.DPST_13_1 => "Counter Pulses Signed (32-bit)",
                KnxDataPointType.DPST_13_2 => "Flow Rate (l/h)",
                KnxDataPointType.DPST_13_10 => "Active Energy (Wh)",
                KnxDataPointType.DPST_13_13 => "Active Energy (kWh)",

                // 32-bit float (DPT 14)
                KnxDataPointType.DPST_14_1 => "Acceleration (m/s²)",
                KnxDataPointType.DPST_14_19 => "Electric Current (A)",
                KnxDataPointType.DPST_14_27 => "Voltage (V)",
                KnxDataPointType.DPST_14_33 => "Frequency (Hz)",
                KnxDataPointType.DPST_14_56 => "Power (W)",

                // String (DPT 16)
                KnxDataPointType.DPST_16_0 => "String (ASCII)",
                KnxDataPointType.DPST_16_1 => "String (ISO-8859-1)",

                // Scene (DPT 17, 18)
                KnxDataPointType.DPST_17_1 => "Scene Number",
                KnxDataPointType.DPST_18_1 => "Scene Control",

                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get classic KNX data type (backward compatibility)
        /// </summary>
        public static string GetClassicDataType(this KnxDataPointType dpt)
        {
            return dpt switch
            {
                // Boolean (1 bit)
                >= KnxDataPointType.DPST_1_1 and <= KnxDataPointType.DPST_1_24 => "Boolean",

                // 4-bit control
                KnxDataPointType.DPST_3_7 or KnxDataPointType.DPST_3_8 => "4-bit Control",

                // 8-bit unsigned
                >= KnxDataPointType.DPST_5_1 and <= KnxDataPointType.DPST_5_10 => "8-bit Unsigned",

                // 8-bit signed
                >= KnxDataPointType.DPST_6_1 and <= KnxDataPointType.DPST_6_20 => "8-bit Signed",

                // 16-bit unsigned
                >= KnxDataPointType.DPST_7_1 and <= KnxDataPointType.DPST_7_13 => "16-bit Unsigned",

                // 16-bit signed
                >= KnxDataPointType.DPST_8_1 and <= KnxDataPointType.DPST_8_10 => "16-bit Signed",

                // 16-bit float
                >= KnxDataPointType.DPST_9_1 and <= KnxDataPointType.DPST_9_24 => "16-bit Float",

                // Time
                KnxDataPointType.DPST_10_1 => "Time",

                // Date
                KnxDataPointType.DPST_11_1 => "Date",

                // 32-bit unsigned
                KnxDataPointType.DPST_12_1 => "32-bit Unsigned",

                // 32-bit signed
                >= KnxDataPointType.DPST_13_1 and <= KnxDataPointType.DPST_13_13 => "32-bit Signed",

                // 32-bit float
                >= KnxDataPointType.DPST_14_1 and <= KnxDataPointType.DPST_14_56 => "32-bit Float",

                // String
                KnxDataPointType.DPST_16_0 or KnxDataPointType.DPST_16_1 => "String",

                // Scene
                KnxDataPointType.DPST_17_1 or KnxDataPointType.DPST_18_1 => "Scene",

                // Date and Time
                KnxDataPointType.DPST_19_1 => "DateTime",

                _ => "Unknown"
            };
        }
    }
}
