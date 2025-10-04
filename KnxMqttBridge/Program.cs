using System.Xml.Serialization;
using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services;
using KnxMqttBridge.Services.Abstractions;

namespace KnxMqttBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // In Program.cs
            builder.Services.AddOptions<GroupAddressInformation>()
                .Configure(config =>
                {
                    var xmlPath = builder.Configuration["KnxConfig:XmlPath"] ?? "GroupAddresses.xml";

                    var serializer = new XmlSerializer(typeof(GroupAddressExport));
                    using var fileStream = File.OpenRead(xmlPath);
                    var xmlExport = (GroupAddressExport)serializer.Deserialize(fileStream);

                    // Convert to simplified model
                    var simplified = GroupAddressInformation.FromXmlExport(xmlExport);

                    config.GroupAddresses = simplified.GroupAddresses;
                });

            builder.Services.Configure<MqttConfiguration>(builder.Configuration.GetSection("Mqtt"));

            builder.Services.AddHostedService<Worker>();
            builder.Services.AddSingleton<IKnxService, KnxService>();
            builder.Services.AddSingleton<IMqttService, MqttService>();

            var host = builder.Build();
            host.Run();
        }
    }
}