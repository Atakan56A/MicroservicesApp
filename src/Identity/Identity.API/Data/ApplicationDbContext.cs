using Identity.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.API.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private int _tenantId = 0; // Guid yerine int tipinde değişken

        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Tenant> Tenants { get; set; } // Eksik olan Tenants DbSet'i eklendi

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, 
                                   IHttpContextAccessor httpContextAccessor = null)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
            
            // Initialize tenant ID from the current user claims if available
            if (_httpContextAccessor != null && 
                _httpContextAccessor.HttpContext != null && 
                _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated)
            {
                var tenantIdClaim = _httpContextAccessor.HttpContext.User.FindFirst("TenantId");
                if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out int tenantId))
                {
                    _tenantId = tenantId;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure ApplicationUser entity
            builder.Entity<ApplicationUser>(entity =>
            {
                // Add global query filter for multi-tenancy - Guid yerine int karşılaştırma
                entity.HasQueryFilter(u => u.TenantId == _tenantId || _tenantId == 0);

                // Configure relationship with RefreshTokens
                entity.HasMany(u => u.RefreshTokens)
                      .WithOne(rt => rt.User)
                      .HasForeignKey(rt => rt.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure RefreshToken entity
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);
                
                // Add global query filter that matches the ApplicationUser filter
                entity.HasQueryFilter(rt => rt.User.TenantId == _tenantId || _tenantId == 0);
                
                entity.Property(rt => rt.Token).IsRequired();
                entity.Property(rt => rt.Expires).IsRequired(); // ExpiryTime değil, Expires kullanılıyor
            });

            // Configure Tenant entity
            builder.Entity<Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                // Tenant'lar tüm kiracılar tarafından görülebilir, filtre yok
            });

            // TenantId için default değer ayarla (Guid.Empty yerine 0)
            builder.Entity<ApplicationUser>()
                .Property(u => u.TenantId)
                .HasDefaultValue(0);
        }

        // Override SaveChanges methods
        public override int SaveChanges()
        {
            UpdateTenantId();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTenantId();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTenantId()
        {
            if (_tenantId != 0) // Guid.Empty yerine 0 kontrolü
            {
                var entries = ChangeTracker.Entries()
                    .Where(e => e.Entity is ApplicationUser && e.State == EntityState.Added);

                foreach (var entry in entries)
                {
                    if (entry.Entity is ApplicationUser user && user.TenantId == 0) // Guid.Empty yerine 0 kontrolü
                    {
                        user.TenantId = _tenantId;
                    }
                }
            }
        }
    }
}