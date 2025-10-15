using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Models;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace KnxMqttBridge.Services
{
    /// <summary>
    /// Service for handling KNX command encoding and DPT resolution
    /// </summary>
    public class KnxCommandHandler : IKnxCommandHandler
    {
        private readonly ILogger<KnxCommandHandler> _logger;
        private readonly IKnxDataPointService _dataPointService;
        private readonly GroupAddressInformation? _groupAddressInformation;

        public KnxCommandHandler(
            ILogger<KnxCommandHandler> logger,
            IKnxDataPointService dataPointService,
            IOptions<GroupAddressInformation>? groupAddressInformation = null)
        {
            _logger = logger;
            _dataPointService = dataPointService;
            _groupAddressInformation = groupAddressInformation?.Value;
        }

        public bool TryResolveDataPointType(string address, KnxCommand command, out string dataPointType)
        {
            dataPointType = string.Empty;

            // Try to get configuration for this address
            var addressConfig = _groupAddressInformation?.GetByAddress(address);

            if (addressConfig != null)
            {
                // We have configuration - use the known DPT
                dataPointType = addressConfig.DataPointType;
                _logger.LogInformation("Found group address: {Name}, DPT: {DataPointType}",
                    addressConfig.Name, dataPointType);
                return true;
            }

            if (!string.IsNullOrEmpty(command.DataPointType))
            {
                // No configuration - use DPT from command payload
                dataPointType = command.DataPointType;
                _logger.LogInformation("Using DPT from command payload: {DataPointType}", dataPointType);
                return true;
            }

            _logger.LogWarning("Address {Address} not configured and no DataPointType provided in command. " +
                             "Command payload must include \"DataPointType\" field.", address);
            return false;
        }

        public bool TryEncodeValue(object commandValue, string dataPointType, out object encodedValue)
        {
            encodedValue = null!;

            // Convert command value to string for encoding
            string valueString = commandValue switch
            {
                JsonElement jsonElement => jsonElement.GetRawText(),
                _ => JsonSerializer.Serialize(commandValue)
            };

            try
            {
                encodedValue = _dataPointService.EncodeValue(valueString, dataPointType);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encode value '{Value}' for DPT {DataPointType}", valueString, dataPointType);
                return false;
            }
        }
    }
}
