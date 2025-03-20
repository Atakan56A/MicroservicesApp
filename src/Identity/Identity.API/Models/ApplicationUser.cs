using Microsoft.AspNetCore.Identity;

namespace Identity.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        // Tenant entegrasyonu için eklenenler
        public Guid TenantId { get; set; }
        public virtual Tenant Tenant { get; set; }
        public bool IsActive { get; set; } = true;
        // RefreshToken iliþkisi (1-N iliþki)
        public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    }
}