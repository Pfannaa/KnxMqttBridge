namespace KnxMqttBridge.Services.Abstractions;

/// <summary>
/// Comprehensive KNX Data Point Type (DPT) encoder and decoder for DPT 1-19
/// </summary>
public interface IKnxDataPointService
{
    object DecodeValue(byte[] rawValue, string? dataPointType = null);
    object EncodeValue(object value, string dataPointType);
    string GetClassicDataType(string dataPointType);
    string GetDataPointDescription(string dataPointType);
}