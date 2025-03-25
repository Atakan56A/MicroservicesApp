using Microsoft.EntityFrameworkCore;
using Tenant.API.Models;

namespace Tenant.API.Data
{
    public class TenantDbContext : DbContext
    {
        public TenantDbContext(DbContextOptions<TenantDbContext> options) : base(options)
        {
        }

        public DbSet<Models.Tenant> Tenants { get; set; }
        public DbSet<TenantSettings> TenantSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Tenant entity
            modelBuilder.Entity<Models.Tenant>(entity =>
            {
                entity.HasKey(t => t.Id);
                entity.Property(t => t.Name).IsRequired();
                entity.Property(t => t.AdminEmail).IsRequired();
                entity.HasOne(t => t.Settings)
                      .WithOne(s => s.Tenant)
                      .HasForeignKey<TenantSettings>(s => s.TenantId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TenantSettings entity
            modelBuilder.Entity<TenantSettings>(entity =>
            {
                entity.HasKey(s => s.Id);
                entity.Property(s => s.Theme).IsRequired();
                entity.Property(s => s.MaxUserCount).IsRequired();
            });
        }
    }
}