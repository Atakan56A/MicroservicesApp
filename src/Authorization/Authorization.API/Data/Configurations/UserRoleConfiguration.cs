using Authorization.API.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Authorization.API.Data.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            builder.ToTable("UserRoles");
            
            builder.HasKey(ur => new { ur.UserId, ur.RoleId, ur.TenantId });
            
            builder.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
                
            // Not: UserId ve TenantId için ilgili entity'ler olmadığından
            // burada ilişki tanımlanmıyor. Bu veriler diğer servislerden geliyor olabilir.
        }
    }
}