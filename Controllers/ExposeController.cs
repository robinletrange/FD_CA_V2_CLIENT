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
            type = "FD_CA_V2_CLIENT",
            serialNumber = AppInfo.SerialNumber,
            version = "1.0.0",
            hostname = Environment.MachineName
        });
    }
}