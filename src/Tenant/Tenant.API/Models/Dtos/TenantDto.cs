namespace Tenant.API.Models.Dtos
{
    public class TenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string AdminEmail { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public TenantSettingsDto Settings { get; set; }
    }
}
