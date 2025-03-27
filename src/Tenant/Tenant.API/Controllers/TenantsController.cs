using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tenant.API.Models;
using Tenant.API.Models.Dtos;
using Tenant.API.Services;

namespace Tenant.API.Controllers
{
    [ApiController]
    [Route("api/tenants")]
    [Authorize]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(ITenantService tenantService, ILogger<TenantsController> logger)
        {
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetTenants()
        {
            _logger.LogInformation("Getting all tenants");
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTenant(Guid id)
        {
            _logger.LogInformation("Getting tenant with ID: {TenantId}", id);
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
                return NotFound();
            return Ok(tenant);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTenant([FromBody] TenantCreateDto model)
        {
            _logger.LogInformation("Creating new tenant");
            var tenantId = await _tenantService.CreateTenantAsync(model);
            return CreatedAtAction(nameof(GetTenant), new { id = tenantId }, null);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] TenantUpdateDto model)
        {
            _logger.LogInformation("Updating tenant with ID: {TenantId}", id);
            await _tenantService.UpdateTenantAsync(id, model);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTenant(Guid id)
        {
            _logger.LogInformation("Deleting tenant with ID: {TenantId}", id);
            await _tenantService.DeleteTenantAsync(id);
            return NoContent();
        }
    }
}
