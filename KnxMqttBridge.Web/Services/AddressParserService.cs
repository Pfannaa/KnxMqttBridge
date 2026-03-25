using System.Xml.Linq;
using KnxMqttBridge.Web.Infrastructure;
using KnxMqttBridge.Web.Models;
using Microsoft.Extensions.Options;

namespace KnxMqttBridge.Web.Services;

public class AddressParserService(IOptions<WebConfiguration> options)
{
    private static readonly XNamespace Ns = "http://knx.org/xml/ga-export/01";

    public List<KnxAddressDto> ParseAddresses()
    {
        var xmlPath = options.Value.XmlPath;

        if (!File.Exists(xmlPath))
            return [];

        var doc = XDocument.Load(xmlPath);
        var addresses = new List<KnxAddressDto>();

        foreach (var topRange in doc.Root?.Elements(Ns + "GroupRange") ?? [])
        {
            var category = topRange.Attribute("Name")?.Value ?? "";

            foreach (var subRange in topRange.Elements(Ns + "GroupRange"))
            {
                var subcategory = subRange.Attribute("Name")?.Value ?? "";

                foreach (var ga in subRange.Elements(Ns + "GroupAddress"))
                {
                    var address = ga.Attribute("Address")?.Value;
                    if (address is null) continue;

                    addresses.Add(new KnxAddressDto(
                        address,
                        ga.Attribute("Name")?.Value ?? address,
                        ga.Attribute("DPTs")?.Value,
                        category,
                        subcategory
                    ));
                }
            }
        }

        return addresses;
    }
}
