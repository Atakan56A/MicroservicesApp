using Microsoft.AspNetCore.Mvc;

namespace Gateway.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { Status = "API Gateway is running." });
        }
    }
}