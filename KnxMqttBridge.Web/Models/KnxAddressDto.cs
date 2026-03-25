namespace KnxMqttBridge.Web.Models;

public record KnxAddressDto(
    string Address,
    string Name,
    string? Dpt,
    string Category,
    string Subcategory
);
