using Microsoft.EntityFrameworkCore;
using Tenant.API.Data;
using Tenant.API.Models;
using Tenant.API.Models.Dtos;

namespace Tenant.API.Services
{
    public class TenantService : ITenantService
    {
        private readonly TenantDbContext _context;

        public TenantService(TenantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<TenantDto>> GetAllTenantsAsync()
        {
            var tenants = await _context.Tenants
                .Include(t => t.Settings)
                .ToListAsync();

            return tenants.Select(t => MapToTenantDto(t));
        }

        public async Task<TenantDto> GetTenantByIdAsync(Guid id)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Settings)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return null;

            return MapToTenantDto(tenant);
        }

        public async Task<TenantDto> CreateTenantAsync(TenantCreateDto tenantDto)
        {
            var tenant = new Models.Tenant
            {
                Name = tenantDto.Name,
                AdminEmail = tenantDto.AdminEmail,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Settings = new TenantSettings
                {
                    Theme = tenantDto.Settings?.Theme ?? "default",
                    MaxUserCount = tenantDto.Settings?.MaxUserCount ?? 10
                }
            };

            _context.Tenants.Add(tenant);
            await _context.SaveChangesAsync();

            return MapToTenantDto(tenant);
        }

        public async Task<TenantDto> UpdateTenantAsync(Guid id, TenantUpdateDto tenantDto)
        {
            var tenant = await _context.Tenants
                .Include(t => t.Settings)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null)
                return null;

            // Update tenant properties
            tenant.Name = tenantDto.Name ?? tenant.Name;
            tenant.AdminEmail = tenantDto.AdminEmail ?? tenant.AdminEmail;

            // Update settings if provided
            if (tenantDto.Settings != null)
            {
                tenant.Settings.Theme = tenantDto.Settings.Theme ?? tenant.Settings.Theme;
                tenant.Settings.MaxUserCount = tenantDto.Settings.MaxUserCount > 0 
                    ? tenantDto.Settings.MaxUserCount 
                    : tenant.Settings.MaxUserCount;
            }

            await _context.SaveChangesAsync();
            return MapToTenantDto(tenant);
        }

        public async Task<bool> DeleteTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return false;

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return false;

            tenant.IsActive = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateTenantAsync(Guid id)
        {
            var tenant = await _context.Tenants.FindAsync(id);
            if (tenant == null)
                return false;

            tenant.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        private TenantDto MapToTenantDto(Models.Tenant tenant)
        {
            return new TenantDto
            {
                Id = tenant.Id,
                Name = tenant.Name,
                AdminEmail = tenant.AdminEmail,
                IsActive = tenant.IsActive,
                CreatedAt = tenant.CreatedAt,
                Settings = new TenantSettingsDto
                {
                    Theme = tenant.Settings?.Theme,
                    MaxUserCount = tenant.Settings?.MaxUserCount ?? 0
                }
            };
        }
    }
}