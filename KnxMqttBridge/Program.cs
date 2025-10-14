using System.Xml.Serialization;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;

namespace KnxMqttBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configure KnxConfiguration from appsettings.json and environment variables
            builder.Services.Configure<KnxConfiguration>(builder.Configuration.GetSection("KnxConfig"));

            // Configure MQTT from appsettings.json and environment variables
            builder.Services.Configure<MqttConfiguration>(builder.Configuration.GetSection("Mqtt"));

            // Configure GroupAddressInformation from XML file
            builder.Services.AddOptions<GroupAddressInformation>()
                .Configure<IOptions<KnxConfiguration>>((config, knxConfig) =>
                {
                    var xmlPath = knxConfig.Value.XmlPath ?? "GroupAddresses.xml";

                    var serializer = new XmlSerializer(typeof(GroupAddressExport));
                    using var fileStream = File.OpenRead(xmlPath);
                    var xmlExport = (GroupAddressExport)serializer.Deserialize(fileStream);

                    // Convert to simplified model
                    var simplified = GroupAddressInformation.FromXmlExport(xmlExport);

                    config.GroupAddresses = simplified.GroupAddresses;
                });

            builder.Services.AddHostedService<Worker>();
            builder.Services.AddSingleton<IKnxService, KnxService>();
            builder.Services.AddSingleton<IMqttService, MqttService>();
            builder.Services.AddSingleton<IKnxValueEncoder, KnxValueEncoder>();

            var host = builder.Build();
            host.Run();
        }
    }
}