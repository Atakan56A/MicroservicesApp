using Microsoft.AspNetCore.Mvc;
using Tenant.API.Models.Dtos;
using Tenant.API.Services;

namespace Tenant.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantController : ControllerBase
    {
        private readonly ITenantService _tenantService;
        private readonly ILogger<TenantController> _logger;

        public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
        {
            _tenantService = tenantService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TenantDto>>> GetAllTenants()
        {
            _logger.LogInformation("Getting all tenants");
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TenantDto>> GetTenantById(Guid id)
        {
            _logger.LogInformation("Getting tenant with ID: {TenantId}", id);
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            
            if (tenant == null)
                return NotFound();
                
            return Ok(tenant);
        }

        [HttpPost]
        public async Task<ActionResult<TenantDto>> CreateTenant([FromBody] TenantCreateDto tenantDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            _logger.LogInformation("Creating new tenant");
            var createdTenant = await _tenantService.CreateTenantAsync(tenantDto);
            return CreatedAtAction(nameof(GetTenantById), new { id = createdTenant.Id }, createdTenant);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<TenantDto>> UpdateTenant(Guid id, [FromBody] TenantUpdateDto tenantDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
                
            _logger.LogInformation("Updating tenant with ID: {TenantId}", id);
            var updatedTenant = await _tenantService.UpdateTenantAsync(id, tenantDto);
            
            if (updatedTenant == null)
                return NotFound();
                
            return Ok(updatedTenant);
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTenant(Guid id)
        {
            _logger.LogInformation("Deleting tenant with ID: {TenantId}", id);
            var result = await _tenantService.DeleteTenantAsync(id);
            
            if (!result)
                return NotFound();
                
            return NoContent();
        }

        [HttpPost("{id}/activate")]
        public async Task<ActionResult> ActivateTenant(Guid id)
        {
            _logger.LogInformation("Activating tenant with ID: {TenantId}", id);
            var result = await _tenantService.ActivateTenantAsync(id);
            
            if (!result)
                return NotFound();
                
            return NoContent();
        }

        [HttpPost("{id}/deactivate")]
        public async Task<ActionResult> DeactivateTenant(Guid id)
        {
            _logger.LogInformation("Deactivating tenant with ID: {TenantId}", id);
            var result = await _tenantService.DeactivateTenantAsync(id);
            
            if (!result)
                return NotFound();
                
            return NoContent();
        }
    }
}