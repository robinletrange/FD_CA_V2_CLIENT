namespace CLIENT.Controllers;

[ApiController]
[Route("expose")]
public class ExposeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            type = "CLIENT",
            serialNumber = AppInfo.SerialNumber,
            name = "CLIENT",
            version = "1.0.0",
            hostname = Environment.MachineName
        });
    }
}