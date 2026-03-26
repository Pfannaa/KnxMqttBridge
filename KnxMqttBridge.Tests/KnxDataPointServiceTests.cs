using KnxMqttBridge.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class KnxDataPointServiceTests
    {
        private readonly KnxDataPointService _service;

        public KnxDataPointServiceTests()
        {
            var mockLogger = new Mock<ILogger<KnxDataPointService>>();
            _service = new KnxDataPointService(mockLogger.Object);
        }

        #region EncodeValue Tests

        [Theory]
        [InlineData("DPST-1-1", true)]
        [InlineData("DPST-1-11", false)]
        [InlineData("DPT-1", 1)]
        public void EncodeValue_BooleanTypes_ReturnsCorrectValue(string dpt, object value)
        {
            var result = _service.EncodeValue(value, dpt);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("DPST-5-1", 50)]
        [InlineData("DPST-5-10", 255)]
        [InlineData("DPT-5", 100)]
        public void EncodeValue_UInt8Types_ReturnsCorrectValue(string dpt, object value)
        {
            var result = _service.EncodeValue(value, dpt);
            Assert.NotNull(result);
        }

        [Theory]
        [InlineData("DPST-9-1", 21.5f)]
        [InlineData("DPST-9-4", 1000f)]
        [InlineData("DPT-9", -10.5f)]
        public void EncodeValue_Float16Types_ReturnsCorrectValue(string dpt, object value)
        {
            var result = _service.EncodeValue(value, dpt);
            Assert.NotNull(result);
            Assert.IsType<byte[]>(result);
        }

        [Fact]
        public void EncodeValue_UnsupportedType_ThrowsNotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() =>
                _service.EncodeValue(100, "DPST-999-999"));
        }

        #endregion

        #region DecodeValue Tests

        [Fact]
        public void DecodeValue_BooleanType_ReturnsInteger()
        {
            var rawValue = new byte[] { 0x01 };
            var result = _service.DecodeValue(rawValue, "DPST-1-1");

            Assert.Equal(1, result);
        }

        [Fact]
        public void DecodeValue_BooleanFalse_ReturnsZero()
        {
            var rawValue = new byte[] { 0x00 };
            var result = _service.DecodeValue(rawValue, "DPST-1-1");

            Assert.Equal(0, result);
        }

        [Fact]
        public void DecodeValue_PercentageType_ReturnsPercentage()
        {
            var rawValue = new byte[] { 128 }; // ~50%
            var result = _service.DecodeValue(rawValue, "DPST-5-1");

            Assert.IsType<double>(result);
            var percentage = (double)result;
            Assert.InRange(percentage, 49.0, 51.0);
        }

        [Fact]
        public void DecodeValue_Float16_ReturnsFloat()
        {
            // Encode 21.5°C
            var encoded = new byte[] { 0x0C, 0x1A }; // Approximate encoding
            var result = _service.DecodeValue(encoded, "DPST-9-1");

            Assert.IsType<float>(result);
        }

        [Fact]
        public void DecodeValue_NullOrEmpty_ReturnsNull()
        {
            var result1 = _service.DecodeValue(null, "DPST-1-1");
            var result2 = _service.DecodeValue(new byte[0], "DPST-1-1");

            Assert.Null(result1);
            Assert.Null(result2);
        }

        [Fact]
        public void DecodeValue_WithoutDpt_UsesHeuristic()
        {
            var rawValue = new byte[] { 0x01 };
            var result = _service.DecodeValue(rawValue);

            Assert.NotNull(result);
            Assert.IsType<int>(result);
            Assert.Equal(1, (int)result); // Should detect as boolean and return int
        }

        #endregion

        #region EncodeUInt8 Percentage Tests

        [Theory]
        [InlineData(0, 0)]
        [InlineData(50, 128)]
        [InlineData(100, 255)]
        [InlineData(25, 64)]
        [InlineData(75, 191)]
        public void EncodeValue_PercentageConversion_CorrectMapping(int percentage, int expectedByte)
        {
            var result = _service.EncodeValue(percentage, "DPST-5-1");

            Assert.IsType<byte>(result);
            var byteResult = (byte)result;
            // Allow ±1 for rounding differences (use Math.Max/Min to prevent byte overflow)
            var minValue = Math.Max(0, expectedByte - 1);
            var maxValue = Math.Min(255, expectedByte + 1);
            Assert.InRange(byteResult, minValue, maxValue);
        }

        [Theory]
        [InlineData(-10, 0)]  // Should clamp to 0
        [InlineData(150, 255)] // Should clamp to 100 -> 255
        public void EncodeValue_PercentageOutOfRange_Clamps(int percentage, int expectedByte)
        {
            var result = _service.EncodeValue(percentage, "DPST-5-1");

            Assert.IsType<byte>(result);
            Assert.Equal((byte)expectedByte, (byte)result);
        }

        #endregion

        #region DecodeHeuristic Tests

        [Fact]
        public void DecodeValue_OneByteBoolean_ReturnsInteger()
        {
            var result = _service.DecodeValue(new byte[] { 0x01 });
            Assert.IsType<int>(result);
            Assert.Equal(1, (int)result);
        }

        [Fact]
        public void DecodeValue_OneByteNonBoolean_ReturnsByte()
        {
            var result = _service.DecodeValue(new byte[] { 0x42 });
            Assert.Equal((byte)0x42, result);
        }

        [Fact]
        public void DecodeValue_TwoBytes_ReturnsFloat16()
        {
            var result = _service.DecodeValue(new byte[] { 0x0C, 0x1A });
            Assert.IsType<float>(result);
        }

        [Fact]
        public void DecodeValue_ThreeBytes_ReturnsBase64()
        {
            var result = _service.DecodeValue(new byte[] { 0x01, 0x02, 0x03 });
            Assert.IsType<string>(result);
        }

        [Fact]
        public void DecodeValue_FourBytes_ReturnsFloat32()
        {
            var result = _service.DecodeValue(new byte[] { 0x42, 0x48, 0x00, 0x00 });
            Assert.IsType<float>(result);
        }

        [Fact]
        public void DecodeValue_LongBytes_ReturnsBase64()
        {
            var result = _service.DecodeValue(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 });
            Assert.IsType<string>(result);
        }

        [Fact]
        public void DecodeFloat32_DoesNotMutateInputArray()
        {
            var original = new byte[] { 0x42, 0x48, 0x00, 0x00 }; // 50.0f in big-endian IEEE 754
            var copy = original.ToArray();

            _service.DecodeValue(original, "DPST-14-1");

            Assert.Equal(copy, original); // array should NOT have been reversed
        }

        [Fact]
        public void DecodeValue_Dpt10_IsNotDecodedAsBoolean()
        {
            var data = new byte[] { 0x0A, 0x1E, 0x3C }; // 10:30:60 as time
            var result = _service.DecodeValue(data, "DPT-10");

            // Should NOT be 0 or 1 (boolean), should be a time string
            Assert.IsType<string>(result);
        }

        [Fact]
        public void DecodeValue_Dpt1_DecodesAsBoolean()
        {
            Assert.Equal(1, _service.DecodeValue(new byte[] { 0x01 }, "DPT-1"));
            Assert.Equal(0, _service.DecodeValue(new byte[] { 0x00 }, "DPT-1"));
        }

        #endregion

        #region All DPT Type Coverage

        [Theory]
        [InlineData("DPST-1-1")]
        [InlineData("DPST-2-1")]
        [InlineData("DPST-3-7")]
        [InlineData("DPST-4-1")]
        [InlineData("DPST-5-1")]
        [InlineData("DPST-6-1")]
        [InlineData("DPST-7-1")]
        [InlineData("DPST-8-1")]
        [InlineData("DPST-9-1")]
        [InlineData("DPST-10-1")]
        [InlineData("DPST-11-1")]
        [InlineData("DPST-12-1")]
        [InlineData("DPST-13-1")]
        [InlineData("DPST-14-1")]
        [InlineData("DPST-16-0")]
        [InlineData("DPST-17-1")]
        [InlineData("DPST-18-1")]
        [InlineData("DPST-19-1")]
        public void EncodeValue_AllSupportedTypes_DoesNotThrow(string dpt)
        {
            var exception = Record.Exception(() => _service.EncodeValue(GetSampleValueForDpt(dpt), dpt));
            Assert.Null(exception);
        }

        private object GetSampleValueForDpt(string dpt)
        {
            if (dpt.StartsWith("DPST-1-")) return true;
            if (dpt.StartsWith("DPST-2-")) return 0;
            if (dpt.StartsWith("DPST-3-")) return 0;
            if (dpt.StartsWith("DPST-4-")) return 'A';
            if (dpt.StartsWith("DPST-5-")) return 100;
            if (dpt.StartsWith("DPST-6-")) return 50;
            if (dpt.StartsWith("DPST-7-")) return 1000;
            if (dpt.StartsWith("DPST-8-")) return -100;
            if (dpt.StartsWith("DPST-9-")) return 21.5f;
            if (dpt.StartsWith("DPST-10-")) return "12:30:00";
            if (dpt.StartsWith("DPST-11-")) return "2024-01-01";
            if (dpt.StartsWith("DPST-12-")) return 10000;
            if (dpt.StartsWith("DPST-13-")) return -5000;
            if (dpt.StartsWith("DPST-14-")) return 3.14f;
            if (dpt.StartsWith("DPST-16-")) return "Test";
            if (dpt.StartsWith("DPST-17-")) return (byte)5;
            if (dpt.StartsWith("DPST-18-")) return (byte)10;
            if (dpt.StartsWith("DPST-19-")) return DateTime.Now;
            return 0;
        }

        #endregion
    }
}
