using Microsoft.AspNetCore.Identity;

namespace Identity.API.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int? TenantId { get; set; }
        public bool IsActive { get; set; } = true;
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}