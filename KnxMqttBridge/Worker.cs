using System.Text.Json;
using Knx.Falcon;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace KnxMqttBridge
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IKnxService _knxService;
        private readonly IMqttService _mqttService;
        private readonly IOptions<GroupAddressInformation> _groupAddressInformation;


        public Worker(ILogger<Worker> logger, IKnxService knxService, IMqttService mqttService, IOptions<GroupAddressInformation> groupAddressInformation)
        {
            _logger = logger;
            _knxService = knxService;
            _mqttService = mqttService;
            _groupAddressInformation = groupAddressInformation;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await _knxService.StartListening(cancellationToken);
            _knxService.GroupMessageReceived += KnxGroupMessageReceived;

            await _mqttService.ConnectAsync(cancellationToken);
        }

        private async void KnxGroupMessageReceived(object? sender, GroupEventArgs e)
        {
            var groupAddressInfo = _groupAddressInformation.Value.GetByAddress(e.DestinationAddress);
            groupAddressInfo.RawValue = e.Value.Value;
            groupAddressInfo.Value = groupAddressInfo.GetDecodedValue();
            groupAddressInfo.LastUpdated = DateTime.Now;

            var payload = JsonSerializer.Serialize(groupAddressInfo);

            await _mqttService.PublishAsync($"{groupAddressInfo.Subcategory}/{groupAddressInfo.Category}/{groupAddressInfo.Name}", payload, true);
        }
    }
}
