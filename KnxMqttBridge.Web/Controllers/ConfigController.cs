using Microsoft.AspNetCore.Mvc;
using KnxMqttBridge.Web.Models;
using KnxMqttBridge.Web.Services;

namespace KnxMqttBridge.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController(ConfigService configService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(configService.Load());

    [HttpPost]
    public IActionResult Post([FromBody] AppConfig config)
    {
        configService.Save(config);
        return Ok();
    }
}
