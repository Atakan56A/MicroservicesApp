using System;
using MicroservicesApp.Shared.Interfaces;

namespace Authorization.API.Data.Entities
{
    public abstract class BaseEntity : ITenantEntity
    {
        public Guid Id { get; set; }
        public int TenantId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
    }
}