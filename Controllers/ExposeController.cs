using System.Xml;

namespace CLIENT.Controllers;

[ApiController]
[Route("expose")]
public class ExposeController : ControllerBase
{
    [HttpGet()]
    public IActionResult Get()
    {
        return Ok(new
        {
            serialNumber = AppInfo.SerialNumber
        });
    }
}