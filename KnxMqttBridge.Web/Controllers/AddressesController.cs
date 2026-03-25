using Microsoft.AspNetCore.Mvc;
using KnxMqttBridge.Web.Services;

namespace KnxMqttBridge.Web.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AddressesController(AddressParserService parserService) : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(parserService.ParseAddresses());
}
