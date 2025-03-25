using Microsoft.AspNetCore.Http;

namespace Authorization.API.Services
{
    public interface ITenantAccessService
    {
        int GetCurrentTenantId();
        bool IsTenantActive();
    }

    public class TenantAccessService : ITenantAccessService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public TenantAccessService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        
        public int GetCurrentTenantId()
        {
            if (_httpContextAccessor.HttpContext == null || !_httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
                return -1; // Varsayılan değer
                
            var tenantClaim = _httpContextAccessor.HttpContext.User.FindFirst("TenantId");
            if (tenantClaim != null && int.TryParse(tenantClaim.Value, out int tenantId))
                return tenantId;
                
            return -1;
        }
        
        public bool IsTenantActive()
        {
            var tenantId = GetCurrentTenantId();
            if (tenantId <= 0) 
                return false;
                
            // İleride tenant aktiflik kontrolü yapılabilir
            return true; 
        }
    }
}