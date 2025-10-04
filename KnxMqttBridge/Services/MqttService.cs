using Microsoft.Extensions.Options;
using System.Text;
using MQTTnet;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services.Abstractions;

namespace KnxMqttBridge.Services
{
    public class MqttService : IDisposable, IMqttService
    {
        private readonly IOptions<MqttConfiguration> _config;
        private readonly ILogger<MqttService> _logger;
        private readonly IMqttClient _mqttClient;
        private bool _disposed;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;

        public MqttService(IOptions<MqttConfiguration> config, ILogger<MqttService> logger)
        {
            _config = config;
            _logger = logger;

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
            _mqttClient.ConnectedAsync += OnConnectedAsync;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                _logger.LogWarning("MQTT client is already connected");
                return;
            }

            try
            {
                var cfg = _config.Value;
                var optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(cfg.BrokerHost, cfg.BrokerPort)
                    .WithClientId(cfg.ClientId)
                    .WithCleanSession(cfg.CleanSession)
                    .WithKeepAlivePeriod(TimeSpan.FromSeconds(cfg.KeepAlivePeriod));

                if (!string.IsNullOrEmpty(cfg.Username))
                {
                    optionsBuilder.WithCredentials(cfg.Username, cfg.Password);
                }

                _logger.LogInformation("Connecting to MQTT broker at {Host}:{Port}", cfg.BrokerHost, cfg.BrokerPort);

                var result = await _mqttClient.ConnectAsync(optionsBuilder.Build(), cancellationToken);

                if (result.ResultCode == MqttClientConnectResultCode.Success)
                {
                    _logger.LogInformation("Successfully connected to MQTT broker");
                }
                else
                {
                    _logger.LogError("Failed to connect to MQTT broker. Result: {Result}", result.ResultCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to MQTT broker");
                throw;
            }
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                _logger.LogInformation("Disconnecting from MQTT broker");
                await _mqttClient.DisconnectAsync(cancellationToken: cancellationToken);
            }
        }

        public async Task PublishAsync(string topic, string payload, bool retain = false, CancellationToken cancellationToken = default)
        {
            await PublishAsync(topic, Encoding.UTF8.GetBytes(payload), retain, cancellationToken);
        }

        public async Task PublishAsync(string topic, byte[] payload, bool retain = false, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected");
            }

            var fullTopic = string.IsNullOrEmpty(_config.Value.TopicPrefix) ? topic : $"{_config.Value.TopicPrefix}/{topic}";

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(fullTopic)
                .WithPayload(payload)
                .WithRetainFlag(retain)
                .Build();

            var result = await _mqttClient.PublishAsync(message, cancellationToken);

            if (result.ReasonCode != MqttClientPublishReasonCode.Success)
            {
                _logger.LogWarning("Failed to publish to {Topic}. Reason: {Reason}", fullTopic, result.ReasonCode);
            }
        }

        private Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("MQTT client connected");
            return Task.CompletedTask;
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("MQTT client disconnected. Reason: {Reason}", args.Reason);

            if (!_disposed && args.Reason != MqttClientDisconnectReason.NormalDisconnection)
            {
                _logger.LogInformation("Attempting to reconnect in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5));

                try
                {
                    await ConnectAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect to MQTT broker");
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _mqttClient?.Dispose();
            }
        }
    }
}
