namespace Tenant.API.Models
{
    public class TenantSettings
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid TenantId { get; set; }
        public string Theme { get; set; }  // Örneğin: "light", "dark"
        public int MaxUserCount { get; set; }
        
        // İlişkisel property
        public Tenant Tenant { get; set; }
    }
}
