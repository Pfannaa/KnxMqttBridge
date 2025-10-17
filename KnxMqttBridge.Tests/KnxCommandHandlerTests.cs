using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class KnxCommandHandlerTests
    {
        private readonly Mock<ILogger<KnxCommandHandler>> _mockLogger;
        private readonly Mock<IKnxDataPointService> _mockDataPointService;
        private readonly KnxCommandHandler _handler;

        public KnxCommandHandlerTests()
        {
            _mockLogger = new Mock<ILogger<KnxCommandHandler>>();
            _mockDataPointService = new Mock<IKnxDataPointService>();
            _handler = new KnxCommandHandler(_mockLogger.Object, _mockDataPointService.Object);
        }

        #region TryResolveDataPointType Tests

        [Fact]
        public void TryResolveDataPointType_WithoutConfig_UsesCommandDPT_ReturnsTrue()
        {
            var command = new KnxCommand
            {
                Value = 100,
                DataPointType = "DPST-5-1"
            };

            var result = _handler.TryResolveDataPointType("1/2/3", command, out var dpt);

            Assert.True(result);
            Assert.Equal("DPST-5-1", dpt);
        }

        [Fact]
        public void TryResolveDataPointType_WithoutConfigOrCommandDPT_ReturnsFalse()
        {
            var command = new KnxCommand
            {
                Value = 100
            };

            var result = _handler.TryResolveDataPointType("1/2/3", command, out var dpt);

            Assert.False(result);
            Assert.Equal(string.Empty, dpt);
        }

        [Fact]
        public void TryResolveDataPointType_WithConfig_UsesConfigDPT_ReturnsTrue()
        {
            // Setup group address information
            var groupAddressInfo = new GroupAddressInformation();
            groupAddressInfo.GroupAddresses.Add(new KnxGroupAddress
            {
                Address = "1/2/3",
                Name = "Test Light",
                DataPointType = "DPST-1-1",
                Category = "Test",
                Subcategory = "Light",
                FullPath = "Test/Light"
            });

            var options = Options.Create(groupAddressInfo);
            var handlerWithConfig = new KnxCommandHandler(
                _mockLogger.Object,
                _mockDataPointService.Object,
                options);

            var command = new KnxCommand
            {
                Value = 1,
                DataPointType = "DPST-5-1" // Should be ignored, config takes precedence
            };

            var result = handlerWithConfig.TryResolveDataPointType("1/2/3", command, out var dpt);

            Assert.True(result);
            Assert.Equal("DPST-1-1", dpt); // Config DPT, not command DPT
        }

        #endregion

        #region TryEncodeValue Tests

        [Fact]
        public void TryEncodeValue_ValidValue_ReturnsTrue()
        {
            _mockDataPointService
                .Setup(s => s.EncodeValue(It.IsAny<object>(), "DPST-1-1"))
                .Returns(true);

            var result = _handler.TryEncodeValue("1", "DPST-1-1", out var encoded);

            Assert.True(result);
            Assert.NotNull(encoded);
        }

        [Fact]
        public void TryEncodeValue_ServiceThrowsException_ReturnsFalse()
        {
            _mockDataPointService
                .Setup(s => s.EncodeValue(It.IsAny<object>(), "DPST-999-999"))
                .Throws(new NotSupportedException("Unsupported DPT"));

            var result = _handler.TryEncodeValue("100", "DPST-999-999", out var encoded);

            Assert.False(result);
        }

        [Fact]
        public void TryEncodeValue_CallsDataPointServiceWithCorrectParameters()
        {
            var testValue = "42";
            var testDpt = "DPST-5-1";

            _mockDataPointService
                .Setup(s => s.EncodeValue(It.IsAny<object>(), testDpt))
                .Returns((byte)42);

            _handler.TryEncodeValue(testValue, testDpt, out var encoded);

            _mockDataPointService.Verify(
                s => s.EncodeValue(It.IsAny<string>(), testDpt),
                Times.Once);
        }

        #endregion
    }
}
