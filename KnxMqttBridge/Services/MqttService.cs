using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using MQTTnet;
using System.Text;

namespace KnxMqttBridge.Services
{
    public class MqttService : IDisposable, IMqttService
    {
        private readonly IOptions<MqttConfiguration> _config;
        private readonly ILogger<MqttService> _logger;
        private readonly IMqttClient _mqttClient;
        private readonly List<string> _subscriptions = [];
        private readonly object _subscriptionsLock = new();
        private bool _disposed;

        public bool IsConnected => _mqttClient?.IsConnected ?? false;
        public event Func<MqttApplicationMessageReceivedEventArgs, Task> MessageReceived;

        public MqttService(IOptions<MqttConfiguration> config, ILogger<MqttService> logger)
        {
            _config = config;
            _logger = logger;

            var factory = new MqttClientFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ConnectedAsync += OnConnectedAsync;
            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;
        }

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected)
            {
                _logger.LogWarning("MQTT client is already connected");
                return;
            }

            while (!IsConnected)
            {
                try
                {
                    var cfg = _config.Value;
                    var optionsBuilder = new MqttClientOptionsBuilder()
                        .WithTcpServer(cfg.BrokerHost, cfg.BrokerPort)
                        .WithClientId(cfg.ClientId + "-" + Guid.NewGuid())
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
                        _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
                    }
                    else
                    {
                        _logger.LogError("Failed to connect to MQTT broker. Result: {Result}", result.ResultCode);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error connecting to MQTT broker");
                    _logger.LogInformation("Retrying connection in 5 seconds...");

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                }
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
                _logger.LogWarning("Dropping publish to {Topic}: MQTT client is not connected", topic);
                return;
            }

            try
            {
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to topic: {Topic}", topic);
                throw;
            }
        }

        private async Task OnConnectedAsync(MqttClientConnectedEventArgs args)
        {
            _logger.LogInformation("MQTT client connected");

            List<string> topicsToRestore;
            lock (_subscriptionsLock)
            {
                if (_subscriptions.Count == 0)
                {
                    return;
                }
                topicsToRestore = [.. _subscriptions];
            }

            _logger.LogInformation("Restoring {Count} MQTT subscription(s) after reconnect", topicsToRestore.Count);
            foreach (var topic in topicsToRestore)
            {
                try
                {
                    var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(topic)
                        .Build();
                    await _mqttClient.SubscribeAsync(subscribeOptions, CancellationToken.None);
                    _logger.LogInformation("Restored subscription to {Topic}", topic);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to restore subscription to {Topic}", topic);
                }
            }
        }

        private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
        {
            _logger.LogWarning("MQTT client disconnected. Reason: {Reason}", args.Reason);

            try
            {
                if (_disposed || args.Reason == MqttClientDisconnectReason.NormalDisconnection)
                {
                    return;
                }

                _logger.LogInformation("Attempting to reconnect in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);

                try
                {
                    await ConnectAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reconnect to MQTT broker.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MQTT disconnection handler");
            }

        }

        public async Task SubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("MQTT client is not connected");
            }

            var fullTopic = string.IsNullOrEmpty(_config.Value.TopicPrefix) ? topic : $"{_config.Value.TopicPrefix}/{topic}";

            lock (_subscriptionsLock)
            {
                if (!_subscriptions.Contains(fullTopic))
                {
                    _subscriptions.Add(fullTopic);
                }
            }

            _logger.LogInformation("Subscribing to MQTT topic: {Topic}", fullTopic);

            var subscribeOptions = new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(fullTopic)
                .Build();

            var result = await _mqttClient.SubscribeAsync(subscribeOptions, cancellationToken);

            foreach (var item in result.Items)
            {
                if (item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS0 ||
                    item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS1 ||
                    item.ResultCode == MqttClientSubscribeResultCode.GrantedQoS2)
                {
                    _logger.LogInformation("Successfully subscribed to {Topic}", item.TopicFilter.Topic);
                }
                else
                {
                    _logger.LogWarning("Failed to subscribe to {Topic}. Result: {Result}", item.TopicFilter.Topic, item.ResultCode);
                }
            }
        }

        private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            try
            {
                _logger.LogDebug("Received message on topic: {Topic}", args.ApplicationMessage.Topic);
                if (MessageReceived != null)
                {
                    await MessageReceived.Invoke(args);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing MQTT message on topic: {Topic}", args.ApplicationMessage.Topic);
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
