using System;

namespace Authorization.API.Data.Entities
{
    public class UserRole : BaseEntity
    {
        public string UserId { get; set; } // Guid'den string'e çevirdik - Identity ile uyumlu
        public Guid RoleId { get; set; }
        
        // Navigation property
        public Role Role { get; set; }
    }
}