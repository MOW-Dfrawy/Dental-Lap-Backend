using Microsoft.AspNetCore.Mvc;

namespace DentalLab.API.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "healthy",
        service = "DentalLab.API",
        version = "1.0.0",
        timestamp = DateTime.UtcNow
    });
}
