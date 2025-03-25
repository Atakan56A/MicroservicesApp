using Authorization.API.Data.Entities;
using Authorization.API.Services;
using Microsoft.EntityFrameworkCore;
using MicroservicesApp.Shared.Constants;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Authorization.API.Data.Context
{
    public class AuthorizationDbContext : DbContext
    {
        private readonly ITenantAccessService _tenantAccessService;
        private readonly int _tenantId;

        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        public AuthorizationDbContext(
            DbContextOptions<AuthorizationDbContext> options,
            ITenantAccessService tenantAccessService = null) : base(options)
        {
            _tenantAccessService = tenantAccessService;
            _tenantId = _tenantAccessService?.GetCurrentTenantId() ?? -1;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Multi-tenant filtreleri - sadece kendi tenant'ınıza ait verileri görün
            // Sistem tenant'ı (0) herkese görünür olsun
            modelBuilder.Entity<Role>().HasQueryFilter(r => 
                r.TenantId == _tenantId || r.TenantId == IdentityConstants.SYSTEM_TENANT_ID);
            
            modelBuilder.Entity<Permission>().HasQueryFilter(p => 
                p.TenantId == _tenantId || p.TenantId == IdentityConstants.SYSTEM_TENANT_ID);
            
            modelBuilder.Entity<RolePermission>().HasQueryFilter(rp => 
                rp.TenantId == _tenantId || rp.TenantId == IdentityConstants.SYSTEM_TENANT_ID);
            
            modelBuilder.Entity<UserRole>().HasQueryFilter(ur => 
                ur.TenantId == _tenantId);
        }

        public override int SaveChanges()
        {
            ApplyTenantFilter();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            ApplyTenantFilter();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void ApplyTenantFilter()
        {
            // Tenant ID geçerli değilse işlem yapmayız
            if (_tenantId <= 0)
                return;

            // Sadece yeni eklenen veya değiştirilen ve ITenantEntity interface'ini
            // implemente eden varlıklara tenant ID'yi otomatik atıyoruz
            foreach (var entry in ChangeTracker.Entries()
                .Where(e => (e.State == EntityState.Added) && e.Entity is BaseEntity))
            {
                var entity = entry.Entity as BaseEntity;
                
                if (entry.State == EntityState.Added)
                {
                    entity.TenantId = _tenantId;
                }
                // Değişiklikte tenant ID'yi değiştirmeye izin vermiyoruz
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property("TenantId").IsModified = false;
                }
            }
        }
    }
}