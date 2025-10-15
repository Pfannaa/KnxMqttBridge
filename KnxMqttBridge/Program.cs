using KnxMqttBridge.Infrastructure;
using KnxMqttBridge.Services;
using KnxMqttBridge.Services.Abstractions;
using Microsoft.Extensions.Options;
using System.Xml.Serialization;

namespace KnxMqttBridge
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Configure KnxConfiguration from appsettings.json and environment variables
            builder.Services.Configure<KnxConfiguration>(builder.Configuration.GetSection("KnxConfig"));

            // Validate KnxConfiguration
            builder.Services.AddOptions<KnxConfiguration>()
                .Validate(config =>
                {
                    // Validate AddressStyle enum
                    if (!Enum.IsDefined(typeof(KnxAddressStyle), config.AddressStyle))
                    {
                        return false;
                    }
                    return true;
                }, $"Invalid AddressStyle. Must be '{nameof(KnxAddressStyle.TwoLevel)}' or '{nameof(KnxAddressStyle.ThreeLevel)}'");

            // Configure MQTT from appsettings.json and environment variables
            builder.Services.Configure<MqttConfiguration>(builder.Configuration.GetSection("Mqtt"));

            // Configure GroupAddressInformation from XML file (OPTIONAL)
            // If the file doesn't exist, the bridge will work with heuristic decoding
            builder.Services.AddOptions<GroupAddressInformation>()
                .Configure<IOptions<KnxConfiguration>>((config, knxConfig) =>
                {
                    var xmlPath = knxConfig.Value.XmlPath ?? "GroupAddresses.xml";

                    if (File.Exists(xmlPath))
                    {
                        try
                        {
                            var serializer = new XmlSerializer(typeof(GroupAddressExport));
                            using var fileStream = File.OpenRead(xmlPath);
                            var xmlExport = (GroupAddressExport)serializer.Deserialize(fileStream);

                            // Convert to simplified model
                            var simplified = GroupAddressInformation.FromXmlExport(xmlExport);
                            config.GroupAddresses = simplified.GroupAddresses;

                            Console.WriteLine($"[Program] Loaded {config.GroupAddresses.Count} group addresses from {xmlPath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Program] Warning: Failed to load ETS export from {xmlPath}: {ex.Message}");
                            Console.WriteLine("[Program] Bridge will run without ETS configuration using heuristic decoding");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[Program] ETS export file not found at {xmlPath}");
                        Console.WriteLine("[Program] Bridge will run without ETS configuration using heuristic decoding");
                    }
                });

            // Register services
            builder.Services.AddHostedService<Worker>();
            builder.Services.AddSingleton<IKnxService, KnxService>();
            builder.Services.AddSingleton<IMqttService, MqttService>();
            builder.Services.AddSingleton<IKnxDataPointService, KnxDataPointService>();
            builder.Services.AddSingleton<IMqttTopicParser, MqttTopicParser>();
            builder.Services.AddSingleton<IKnxCommandHandler, KnxCommandHandler>();

            // Keep legacy encoder for backward compatibility (can be removed later)
            builder.Services.AddSingleton<IKnxValueEncoder, KnxValueEncoder>();

            var host = builder.Build();
            host.Run();
        }
    }
}