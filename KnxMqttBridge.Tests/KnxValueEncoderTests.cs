using KnxMqttBridge.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class KnxValueEncoderTests
    {
        private readonly KnxValueEncoder _encoder;
        private readonly Mock<ILogger<KnxValueEncoder>> _mockLogger;

        public KnxValueEncoderTests()
        {
            _mockLogger = new Mock<ILogger<KnxValueEncoder>>();
            _encoder = new KnxValueEncoder(_mockLogger.Object);
        }

        #region Boolean Encoding Tests

        [Theory]
        [InlineData("DPST-1-1", "1", true)]
        [InlineData("DPST-1-1", "0", false)]
        [InlineData("DPST-1-11", "1", true)]
        [InlineData("DPST-1-24", "0", false)]
        public void EncodeValue_BooleanTypes_ReturnsBoolean(string dpt, string value, bool expected)
        {
            var result = _encoder.EncodeValue(value, dpt);

            Assert.NotNull(result);
            Assert.IsType<bool>(result);
            Assert.Equal(expected, (bool)result);
        }

        #endregion

        #region UInt8 Encoding Tests

        [Theory]
        [InlineData("DPST-5-1", "0", (byte)0)]
        [InlineData("DPST-5-1", "128", (byte)128)]
        [InlineData("DPST-5-1", "255", (byte)255)]
        [InlineData("DPST-5-10", "100", (byte)100)]
        public void EncodeValue_UInt8Types_ReturnsByte(string dpt, string value, byte expected)
        {
            var result = _encoder.EncodeValue(value, dpt);

            Assert.NotNull(result);
            Assert.IsType<byte>(result);
            Assert.Equal(expected, (byte)result);
        }

        #endregion

        #region Float16 Encoding Tests

        [Theory]
        [InlineData("DPST-9-1", "21.5")]
        [InlineData("DPST-9-4", "1000.0")]
        [InlineData("DPST-9-5", "5.5")]
        public void EncodeValue_Float16Types_ReturnsByteArray(string dpt, string value)
        {
            var result = _encoder.EncodeValue(value, dpt);

            Assert.NotNull(result);
            Assert.IsType<byte[]>(result);
            var bytes = (byte[])result;
            Assert.Equal(2, bytes.Length);
        }

        [Fact]
        public void EncodeValue_Float16_PositiveValue_EncodesCorrectly()
        {
            var result = _encoder.EncodeValue("21.5", "DPST-9-1");

            Assert.NotNull(result);
            Assert.IsType<byte[]>(result);
            var bytes = (byte[])result;

            // For 21.5°C, sign bit should be 0 (positive)
            // First byte bit 7 should be 0
            Assert.True((bytes[0] & 0x80) == 0);
        }

        [Fact]
        public void EncodeValue_Float16_NegativeValue_EncodesCorrectly()
        {
            var result = _encoder.EncodeValue("-10.5", "DPST-9-1");

            Assert.NotNull(result);
            Assert.IsType<byte[]>(result);
            var bytes = (byte[])result;

            // For negative value, sign bit should be 1
            // First byte bit 7 should be 1
            Assert.True((bytes[0] & 0x80) != 0);
        }

        #endregion

        #region Dimming Control Tests

        [Fact]
        public void EncodeValue_DimmingControl_Up_EncodesCorrectly()
        {
            var json = "{\"Direction\":\"up\",\"Steps\":5}";
            var result = _encoder.EncodeValue(json, "DPST-3-7");

            Assert.NotNull(result);
            var tuple = ((byte, int))result;
            var encoded = tuple.Item1;
            var bitSize = tuple.Item2;

            Assert.Equal(4, bitSize);
            // 0x08 (direction up) | 0x05 (5 steps) = 0x0D
            Assert.Equal(0x0D, encoded);
        }

        [Fact]
        public void EncodeValue_DimmingControl_Down_EncodesCorrectly()
        {
            var json = "{\"Direction\":\"down\",\"Steps\":3}";
            var result = _encoder.EncodeValue(json, "DPST-3-7");

            Assert.NotNull(result);
            var tuple = ((byte, int))result;
            var encoded = tuple.Item1;

            // 0x00 (direction down) | 0x03 (3 steps) = 0x03
            Assert.Equal(0x03, encoded);
        }

        [Fact]
        public void EncodeValue_DimmingControl_MaxSteps_ClampsToSeven()
        {
            var json = "{\"Direction\":\"up\",\"Steps\":15}";
            var result = _encoder.EncodeValue(json, "DPST-3-7");

            Assert.NotNull(result);
            var tuple = ((byte, int))result;
            var encoded = tuple.Item1;

            // Steps should be clamped to 7 (0x07)
            // 0x08 (direction up) | 0x07 (7 steps max) = 0x0F
            Assert.Equal(0x0F, encoded);
        }

        #endregion

        #region Scene Number Tests

        [Theory]
        [InlineData("0", (byte)0)]
        [InlineData("32", (byte)32)]
        [InlineData("63", (byte)63)]
        public void EncodeValue_SceneNumber_ReturnsByte(string value, byte expected)
        {
            var result = _encoder.EncodeValue(value, "DPST-18-1");

            Assert.NotNull(result);
            Assert.IsType<byte>(result);
            Assert.Equal(expected, (byte)result);
        }

        #endregion

        #region Unsupported DPT Tests

        [Fact]
        public void EncodeValue_UnsupportedDPT_ReturnsNull()
        {
            var result = _encoder.EncodeValue("100", "DPST-999-999");

            Assert.Null(result);
        }

        #endregion

        #region Error Handling Tests

        [Fact]
        public void EncodeValue_InvalidNumericValue_ReturnsNull()
        {
            var result = _encoder.EncodeValue("not-a-number", "DPST-5-1");

            Assert.Null(result);
        }

        [Fact]
        public void EncodeValue_InvalidJsonForDimming_ReturnsZero()
        {
            var result = _encoder.EncodeValue("invalid-json", "DPST-3-7");

            Assert.NotNull(result);
            var tuple = ((byte, int))result;
            Assert.Equal((byte)0, tuple.Item1);
        }

        #endregion
    }
}
