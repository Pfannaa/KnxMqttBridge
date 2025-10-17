using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace KnxMqttBridge.Tests
{
    public class MqttTopicParserTests
    {
        private readonly Mock<ILogger<MqttTopicParser>> _mockLogger;
        private readonly MqttTopicParser _parser;

        public MqttTopicParserTests()
        {
            _mockLogger = new Mock<ILogger<MqttTopicParser>>();

            var knxConfig = Options.Create(new KnxConfiguration
            {
                AddressStyle = KnxAddressStyle.ThreeLevel
            });

            var mqttConfig = Options.Create(new MqttConfiguration
            {
                TopicPrefix = "knx"
            });

            _parser = new MqttTopicParser(_mockLogger.Object, knxConfig, mqttConfig);
        }

        #region TryParseAddressFromTopic Tests

        [Theory]
        [InlineData("knx/GroupAddresses/1/2/3/command", "1/2/3")]
        [InlineData("knx/GroupAddresses/0/0/1/command", "0/0/1")]
        [InlineData("knx/GroupAddresses/31/7/255/command", "31/7/255")]
        public void TryParseAddressFromTopic_ThreeLevel_ValidTopic_ReturnsTrue(string topic, string expectedAddress)
        {
            var result = _parser.TryParseAddressFromTopic(topic, out var address);

            Assert.True(result);
            Assert.Equal(expectedAddress, address);
        }

        [Fact]
        public void TryParseAddressFromTopic_TwoLevel_ValidTopic_ReturnsTrue()
        {
            var knxConfig = Options.Create(new KnxConfiguration
            {
                AddressStyle = KnxAddressStyle.TwoLevel
            });

            var mqttConfig = Options.Create(new MqttConfiguration
            {
                TopicPrefix = "knx"
            });

            var parser = new MqttTopicParser(_mockLogger.Object, knxConfig, mqttConfig);

            var result = parser.TryParseAddressFromTopic("knx/GroupAddresses/1/3/command", out var address);

            Assert.True(result);
            Assert.Equal("1/3", address);
        }

        [Fact]
        public void TryParseAddressFromTopic_CustomPrefix_ValidTopic_ReturnsTrue()
        {
            var knxConfig = Options.Create(new KnxConfiguration
            {
                AddressStyle = KnxAddressStyle.ThreeLevel
            });

            var mqttConfig = Options.Create(new MqttConfiguration
            {
                TopicPrefix = "myknx"
            });

            var parser = new MqttTopicParser(_mockLogger.Object, knxConfig, mqttConfig);

            var result = parser.TryParseAddressFromTopic("myknx/GroupAddresses/1/2/3/command", out var address);

            Assert.True(result);
            Assert.Equal("1/2/3", address);
        }

        [Theory]
        [InlineData("wrongprefix/GroupAddresses/1/2/3/command")]
        [InlineData("knx/WrongPath/1/2/3/command")]
        [InlineData("knx/GroupAddresses/1/2/3")] // Missing /command
        [InlineData("knx/GroupAddresses/1/2/3/wrongsuffix")]
        public void TryParseAddressFromTopic_InvalidTopic_ReturnsFalse(string topic)
        {
            var result = _parser.TryParseAddressFromTopic(topic, out var address);

            Assert.False(result);
            Assert.Equal(string.Empty, address);
        }

        [Theory]
        [InlineData("knx/GroupAddresses/1/command")] // Only 1 level
        [InlineData("knx/GroupAddresses/1/2/command")] // Only 2 levels
        [InlineData("knx/GroupAddresses/1/2/3/4/command")] // 4 levels
        public void TryParseAddressFromTopic_ThreeLevel_InvalidAddressFormat_ReturnsFalse(string topic)
        {
            var result = _parser.TryParseAddressFromTopic(topic, out var address);

            Assert.False(result);
            Assert.Equal(string.Empty, address);
        }

        [Theory]
        [InlineData("knx/GroupAddresses/1/command")] // Only 1 level
        [InlineData("knx/GroupAddresses/1/2/3/command")] // 3 levels (too many)
        public void TryParseAddressFromTopic_TwoLevel_InvalidAddressFormat_ReturnsFalse(string topic)
        {
            var knxConfig = Options.Create(new KnxConfiguration
            {
                AddressStyle = KnxAddressStyle.TwoLevel
            });

            var mqttConfig = Options.Create(new MqttConfiguration
            {
                TopicPrefix = "knx"
            });

            var parser = new MqttTopicParser(_mockLogger.Object, knxConfig, mqttConfig);

            var result = parser.TryParseAddressFromTopic(topic, out var address);

            Assert.False(result);
            Assert.Equal(string.Empty, address);
        }

        #endregion

        #region TryDeserializeCommand Tests

        [Fact]
        public void TryDeserializeCommand_ValidJson_ReturnsTrue()
        {
            var json = "{\"Value\":100,\"DataPointType\":\"DPST-5-1\"}";

            var result = _parser.TryDeserializeCommand(json, out var command);

            Assert.True(result);
            Assert.NotNull(command);
            Assert.Equal("DPST-5-1", command.DataPointType);
        }

        [Fact]
        public void TryDeserializeCommand_ValueOnly_ReturnsTrue()
        {
            var json = "{\"Value\":1}";

            var result = _parser.TryDeserializeCommand(json, out var command);

            Assert.True(result);
            Assert.NotNull(command);
            Assert.Null(command.DataPointType);
        }

        [Fact]
        public void TryDeserializeCommand_ComplexValue_ReturnsTrue()
        {
            var json = "{\"Value\":{\"Direction\":\"up\",\"Steps\":5},\"DataPointType\":\"DPST-3-7\"}";

            var result = _parser.TryDeserializeCommand(json, out var command);

            Assert.True(result);
            Assert.NotNull(command);
            Assert.Equal("DPST-3-7", command.DataPointType);
        }

        [Theory]
        [InlineData("")]
        [InlineData("not json")]
        [InlineData("{invalid json}")]
        [InlineData("{\"WrongProperty\":100}")]
        public void TryDeserializeCommand_InvalidJson_ReturnsFalse(string json)
        {
            var result = _parser.TryDeserializeCommand(json, out var command);

            Assert.False(result);
        }

        #endregion
    }
}
