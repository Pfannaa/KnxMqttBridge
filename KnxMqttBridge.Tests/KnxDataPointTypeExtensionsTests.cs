using KnxMqttBridge.Models;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class KnxDataPointTypeExtensionsTests
    {
        #region ParseDataPointType Tests

        [Theory]
        [InlineData("DPST-1-1", KnxDataPointType.DPST_1_1)]
        [InlineData("DPST-5-1", KnxDataPointType.DPST_5_1)]
        [InlineData("DPST-9-1", KnxDataPointType.DPST_9_1)]
        [InlineData("dpst-1-1", KnxDataPointType.DPST_1_1)] // Case insensitive
        public void ParseDataPointType_ValidString_ReturnsEnum(string input, KnxDataPointType expected)
        {
            var result = KnxDataPointTypeExtensions.ParseDataPointType(input);
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("INVALID")]
        public void ParseDataPointType_InvalidString_ReturnsNull(string input)
        {
            var result = KnxDataPointTypeExtensions.ParseDataPointType(input);
            Assert.Null(result);
        }

        #endregion

        #region ToDataPointString Tests

        [Theory]
        [InlineData(KnxDataPointType.DPST_1_1, "DPST-1-1")]
        [InlineData(KnxDataPointType.DPST_5_1, "DPST-5-1")]
        [InlineData(KnxDataPointType.DPST_9_1, "DPST-9-1")]
        public void ToDataPointString_ValidEnum_ReturnsString(KnxDataPointType input, string expected)
        {
            var result = input.ToDataPointString();
            Assert.Equal(expected, result);
        }

        #endregion

        #region GetDescription Tests

        [Theory]
        [InlineData(KnxDataPointType.DPST_1_1, "Switch")]
        [InlineData(KnxDataPointType.DPST_1_11, "State")]
        [InlineData(KnxDataPointType.DPST_5_1, "Scaling (0-255)")]
        [InlineData(KnxDataPointType.DPST_9_1, "Temperature (°C)")]
        [InlineData(KnxDataPointType.DPST_18_1, "Scene Control")]
        public void GetDescription_KnownTypes_ReturnsCorrectDescription(KnxDataPointType dpt, string expected)
        {
            var result = dpt.GetDescription();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetDescription_AllBooleanTypes_ReturnDescriptions()
        {
            Assert.Equal("Switch", KnxDataPointType.DPST_1_1.GetDescription());
            Assert.Equal("Boolean", KnxDataPointType.DPST_1_2.GetDescription());
            Assert.Equal("Enable", KnxDataPointType.DPST_1_3.GetDescription());
            Assert.Equal("OpenClose", KnxDataPointType.DPST_1_9.GetDescription());
            Assert.Equal("Start", KnxDataPointType.DPST_1_10.GetDescription());
            Assert.Equal("State", KnxDataPointType.DPST_1_11.GetDescription());
        }

        [Fact]
        public void GetDescription_AllTemperatureTypes_ReturnDescriptions()
        {
            Assert.Equal("Temperature (°C)", KnxDataPointType.DPST_9_1.GetDescription());
            Assert.Equal("Illuminance (Lux)", KnxDataPointType.DPST_9_4.GetDescription());
            Assert.Equal("Wind Speed (m/s)", KnxDataPointType.DPST_9_5.GetDescription());
        }

        #endregion

        #region GetClassicDataType Tests

        [Theory]
        [InlineData(KnxDataPointType.DPST_1_1, "Boolean")]
        [InlineData(KnxDataPointType.DPST_1_11, "Boolean")]
        [InlineData(KnxDataPointType.DPST_1_24, "Boolean")]
        public void GetClassicDataType_BooleanRange_ReturnsBoolean(KnxDataPointType dpt, string expected)
        {
            var result = dpt.GetClassicDataType();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(KnxDataPointType.DPST_5_1, "8-bit Unsigned")]
        [InlineData(KnxDataPointType.DPST_5_10, "8-bit Unsigned")]
        public void GetClassicDataType_UInt8Range_Returns8BitUnsigned(KnxDataPointType dpt, string expected)
        {
            var result = dpt.GetClassicDataType();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(KnxDataPointType.DPST_9_1, "16-bit Float")]
        [InlineData(KnxDataPointType.DPST_9_24, "16-bit Float")]
        public void GetClassicDataType_Float16Range_Returns16BitFloat(KnxDataPointType dpt, string expected)
        {
            var result = dpt.GetClassicDataType();
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData(KnxDataPointType.DPST_3_7, "4-bit Control")]
        [InlineData(KnxDataPointType.DPST_3_8, "4-bit Control")]
        public void GetClassicDataType_ControlTypes_Returns4BitControl(KnxDataPointType dpt, string expected)
        {
            var result = dpt.GetClassicDataType();
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetClassicDataType_AllRanges_ReturnCorrectTypes()
        {
            // Test boundary conditions for ranges
            Assert.Equal("Boolean", KnxDataPointType.DPST_1_1.GetClassicDataType());
            Assert.Equal("8-bit Unsigned", KnxDataPointType.DPST_5_1.GetClassicDataType());
            Assert.Equal("8-bit Signed", KnxDataPointType.DPST_6_1.GetClassicDataType());
            Assert.Equal("16-bit Unsigned", KnxDataPointType.DPST_7_1.GetClassicDataType());
            Assert.Equal("16-bit Signed", KnxDataPointType.DPST_8_1.GetClassicDataType());
            Assert.Equal("16-bit Float", KnxDataPointType.DPST_9_1.GetClassicDataType());
            Assert.Equal("Time", KnxDataPointType.DPST_10_1.GetClassicDataType());
            Assert.Equal("Date", KnxDataPointType.DPST_11_1.GetClassicDataType());
            Assert.Equal("32-bit Unsigned", KnxDataPointType.DPST_12_1.GetClassicDataType());
            Assert.Equal("32-bit Signed", KnxDataPointType.DPST_13_1.GetClassicDataType());
            Assert.Equal("32-bit Float", KnxDataPointType.DPST_14_1.GetClassicDataType());
            Assert.Equal("String", KnxDataPointType.DPST_16_0.GetClassicDataType());
            Assert.Equal("Scene", KnxDataPointType.DPST_17_1.GetClassicDataType());
            Assert.Equal("DateTime", KnxDataPointType.DPST_19_1.GetClassicDataType());
        }

        #endregion
    }
}
