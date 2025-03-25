using Authorization.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization.API.Controllers
{
    [ApiController]
    [Route("api/authorization/permissions")]
    [Authorize]
    public class PermissionsController : ControllerBase
    {
        private readonly ILogger<PermissionsController> _logger;

        public PermissionsController(ILogger<PermissionsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetPermissions()
        {
            _logger.LogInformation("Getting all permissions");
            // TODO: Implement logic to get all permissions
            return Ok(new List<Permission>());
        }
    }
}
