using Microsoft.AspNetCore.Mvc;

namespace HomeNetMonitor.Api.Controller;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthResponse> Get()
    {
        var response = new HealthResponse(
            Status: "Healthy",
            TimestampUtc: DateTimeOffset.UtcNow
        );

        return Ok(response);
    }
}

public record HealthResponse(
    string Status,
    DateTimeOffset TimestampUtc
);


