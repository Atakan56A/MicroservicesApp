using Authorization.API.Data.Context;
using Authorization.API.Data.Entities;
using Authorization.API.Data.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Authorization.API.Data.Repositories.Implementations
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public RoleRepository(AuthorizationDbContext context) : base(context)
        {
        }

        public async Task<Role> GetRoleWithPermissionsAsync(Guid roleId)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(r => r.Id == roleId);
        }

        public async Task<IEnumerable<Role>> GetRolesByUserIdAsync(string userId, int tenantId)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                .Include(ur => ur.Role)
                .ToListAsync();

            return userRoles.Select(ur => ur.Role);
        }

        public async Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found");
            }

            foreach (var permissionId in permissionIds)
            {
                if (!role.RolePermissions.Any(rp => rp.PermissionId == permissionId))
                {
                    role.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId
                    });
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var role = await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);

            if (role == null)
            {
                throw new InvalidOperationException($"Role with ID {roleId} not found");
            }

            foreach (var permissionId in permissionIds)
            {
                var rolePermission = role.RolePermissions.FirstOrDefault(rp => 
                    rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (rolePermission != null)
                {
                    role.RolePermissions.Remove(rolePermission);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}