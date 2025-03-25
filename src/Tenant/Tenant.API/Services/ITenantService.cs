using Tenant.API.Models;
using Tenant.API.Models.Dtos;

namespace Tenant.API.Services
{
    public interface ITenantService
    {
        Task<IEnumerable<TenantDto>> GetAllTenantsAsync();
        Task<TenantDto> GetTenantByIdAsync(Guid id);
        Task<TenantDto> CreateTenantAsync(TenantCreateDto tenantDto);
        Task<TenantDto> UpdateTenantAsync(Guid id, TenantUpdateDto tenantDto);
        Task<bool> DeleteTenantAsync(Guid id);
        Task<bool> ActivateTenantAsync(Guid id);
        Task<bool> DeactivateTenantAsync(Guid id);
    }
}