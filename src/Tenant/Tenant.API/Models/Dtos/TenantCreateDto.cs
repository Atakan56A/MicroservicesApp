namespace Tenant.API.Models.Dtos
{
    public class TenantCreateDto
    {
        public string Name { get; set; }
        public string AdminEmail { get; set; }
        public TenantSettingsDto Settings { get; set; }
    }
}
