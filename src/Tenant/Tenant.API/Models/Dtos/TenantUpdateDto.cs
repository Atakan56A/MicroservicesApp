namespace Tenant.API.Models.Dtos
{
    public class TenantUpdateDto
    {
        public string Name { get; set; }
        public string AdminEmail { get; set; }
        public bool IsActive { get; set; }
        public TenantSettingsDto Settings { get; set; }
    }
}
