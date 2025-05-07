using Microsoft.AspNetCore.Mvc;

namespace babbly_auth_service.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { status = "Healthy", service = "babbly-auth-service" });
        }
    }
} 