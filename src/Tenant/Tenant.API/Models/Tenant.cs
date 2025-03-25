namespace Tenant.API.Models
{
    public class Tenant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string AdminEmail { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property: Tenant'a ait ayarlar
        public TenantSettings Settings { get; set; }
    }
}
