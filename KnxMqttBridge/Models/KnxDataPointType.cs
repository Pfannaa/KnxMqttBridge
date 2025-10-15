namespace KnxMqttBridge.Models
{
    /// <summary>
    /// KNX Data Point Types (DPT) supported by the bridge
    /// </summary>
    public enum KnxDataPointType
    {
        /// <summary>DPST-1-1: Boolean (Switch)</summary>
        DPST_1_1,

        /// <summary>DPST-1-2: Boolean (True/False)</summary>
        DPST_1_2,

        /// <summary>DPST-1-3: Boolean (Enable)</summary>
        DPST_1_3,

        /// <summary>DPST-1-9: Boolean (Open/Close)</summary>
        DPST_1_9,

        /// <summary>DPST-1-10: Boolean (Start/Stop)</summary>
        DPST_1_10,

        /// <summary>DPST-1-11: Boolean (State)</summary>
        DPST_1_11,

        /// <summary>DPST-1-19: Boolean (Window/Door)</summary>
        DPST_1_19,

        /// <summary>DPST-1-22: Boolean (Dim Send Style)</summary>
        DPST_1_22,

        /// <summary>DPST-1-23: Boolean (Input Source)</summary>
        DPST_1_23,

        /// <summary>DPST-1-24: Boolean (Day/Night)</summary>
        DPST_1_24,

        /// <summary>DPST-3-7: 4-bit dimming control (Direction + Steps)</summary>
        DPST_3_7,

        /// <summary>DPST-3-8: 4-bit blinds control (Direction + Steps)</summary>
        DPST_3_8,

        /// <summary>DPST-5-1: 8-bit unsigned value (0-255, Scaling/Percentage)</summary>
        DPST_5_1,

        /// <summary>DPST-5-3: 8-bit angle (0-360°)</summary>
        DPST_5_3,

        /// <summary>DPST-5-4: 8-bit percentage (0-100%)</summary>
        DPST_5_4,

        /// <summary>DPST-5-5: 8-bit unsigned value (0-255, Decimal Factor)</summary>
        DPST_5_5,

        /// <summary>DPST-5-6: 8-bit unsigned value (0-255, Tariff Information)</summary>
        DPST_5_6,

        /// <summary>DPST-5-10: 8-bit unsigned value (0-255, Counter Pulses)</summary>
        DPST_5_10,

        /// <summary>DPST-6-1: 8-bit signed value (-128 to 127, Percentage)</summary>
        DPST_6_1,

        /// <summary>DPST-6-10: 8-bit signed value (-128 to 127, Counter Pulses)</summary>
        DPST_6_10,

        /// <summary>DPST-6-20: 8-bit status with mode</summary>
        DPST_6_20,

        /// <summary>DPST-7-1: 16-bit unsigned value (0-65535, Pulses)</summary>
        DPST_7_1,

        /// <summary>DPST-7-2: 16-bit unsigned value (milliseconds, Time Period)</summary>
        DPST_7_2,

        /// <summary>DPST-7-3: 16-bit unsigned value (0.01s resolution, Time Period)</summary>
        DPST_7_3,

        /// <summary>DPST-7-4: 16-bit unsigned value (0.1s resolution, Time Period)</summary>
        DPST_7_4,

        /// <summary>DPST-7-5: 16-bit unsigned value (seconds, Time Period)</summary>
        DPST_7_5,

        /// <summary>DPST-7-6: 16-bit unsigned value (minutes, Time Period)</summary>
        DPST_7_6,

        /// <summary>DPST-7-7: 16-bit unsigned value (hours, Time Period)</summary>
        DPST_7_7,

        /// <summary>DPST-7-12: 16-bit unsigned value (mA, Current)</summary>
        DPST_7_12,

        /// <summary>DPST-7-13: 16-bit unsigned value (Lux, Brightness)</summary>
        DPST_7_13,

        /// <summary>DPST-8-1: 16-bit signed value (-32768 to 32767, Pulses)</summary>
        DPST_8_1,

        /// <summary>DPST-8-2: 16-bit signed value (milliseconds, Time Period)</summary>
        DPST_8_2,

        /// <summary>DPST-8-5: 16-bit signed value (seconds, Time Period)</summary>
        DPST_8_5,

        /// <summary>DPST-8-10: 16-bit signed value (%, Percentage)</summary>
        DPST_8_10,

        /// <summary>DPST-9-1: 16-bit float (Temperature °C)</summary>
        DPST_9_1,

        /// <summary>DPST-9-4: 16-bit float (Lux, Illuminance)</summary>
        DPST_9_4,

        /// <summary>DPST-9-5: 16-bit float (m/s, Wind Speed)</summary>
        DPST_9_5,

        /// <summary>DPST-9-6: 16-bit float (Pa, Pressure)</summary>
        DPST_9_6,

        /// <summary>DPST-9-7: 16-bit float (%, Humidity)</summary>
        DPST_9_7,

        /// <summary>DPST-9-8: 16-bit float (ppm, Air Quality)</summary>
        DPST_9_8,

        /// <summary>DPST-9-20: 16-bit float (mV, Voltage)</summary>
        DPST_9_20,

        /// <summary>DPST-9-21: 16-bit float (mA, Current)</summary>
        DPST_9_21,

        /// <summary>DPST-9-24: 16-bit float (kW, Power)</summary>
        DPST_9_24,

        /// <summary>DPST-10-1: Time of day</summary>
        DPST_10_1,

        /// <summary>DPST-11-1: Date</summary>
        DPST_11_1,

        /// <summary>DPST-12-1: 32-bit unsigned value (Counter Pulses)</summary>
        DPST_12_1,

        /// <summary>DPST-13-1: 32-bit signed value (Counter Pulses)</summary>
        DPST_13_1,

        /// <summary>DPST-13-2: 32-bit signed value (l/h, Flow Rate)</summary>
        DPST_13_2,

        /// <summary>DPST-13-10: 32-bit signed value (Wh, Active Energy)</summary>
        DPST_13_10,

        /// <summary>DPST-13-13: 32-bit signed value (kWh, Active Energy)</summary>
        DPST_13_13,

        /// <summary>DPST-14-1: 32-bit float (m/s², Acceleration)</summary>
        DPST_14_1,

        /// <summary>DPST-14-19: 32-bit float (A, Electric Current)</summary>
        DPST_14_19,

        /// <summary>DPST-14-27: 32-bit float (V, Voltage)</summary>
        DPST_14_27,

        /// <summary>DPST-14-33: 32-bit float (Hz, Frequency)</summary>
        DPST_14_33,

        /// <summary>DPST-14-56: 32-bit float (W, Power)</summary>
        DPST_14_56,

        /// <summary>DPST-16-0: String (ASCII)</summary>
        DPST_16_0,

        /// <summary>DPST-16-1: String (ISO-8859-1)</summary>
        DPST_16_1,

        /// <summary>DPST-17-1: Scene number (1-64)</summary>
        DPST_17_1,

        /// <summary>DPST-18-1: Scene control (activate/learn)</summary>
        DPST_18_1,

        /// <summary>DPST-19-1: Date and Time</summary>
        DPST_19_1
    }
}
