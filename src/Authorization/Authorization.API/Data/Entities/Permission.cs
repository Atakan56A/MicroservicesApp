using System;
using System.Collections.Generic;

namespace Authorization.API.Data.Entities
{
    public class Permission : BaseEntity
    {
        public string Name { get; set; }
        public string SystemName { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        
        // Navigation property
        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}