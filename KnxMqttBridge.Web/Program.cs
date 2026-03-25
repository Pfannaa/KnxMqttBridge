using KnxMqttBridge.Web.Infrastructure;
using KnxMqttBridge.Web.Services;

namespace KnxMqttBridge.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure WebConfiguration from appsettings.json and environment variables
            builder.Services.Configure<WebConfiguration>(builder.Configuration.GetSection("Web"));

            // Register controllers
            builder.Services.AddControllers();

            // Register services
            builder.Services.AddSingleton<ConfigService>();
            builder.Services.AddSingleton<AddressParserService>();

            var app = builder.Build();

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapControllers();
            app.MapFallbackToFile("index.html");

            app.Run();
        }
    }
}
