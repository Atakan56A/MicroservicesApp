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
    public class PermissionRepository : Repository<Permission>, IPermissionRepository
    {
        public PermissionRepository(AuthorizationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId)
        {
            var rolePermissions = await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Include(rp => rp.Permission)
                .ToListAsync();

            return rolePermissions.Select(rp => rp.Permission);
        }

        public async Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, Guid tenantId)
        {
            var userRoles = await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                .ToListAsync();

            if (!userRoles.Any())
            {
                return Enumerable.Empty<Permission>();
            }

            var roleIds = userRoles.Select(ur => ur.RoleId).ToList();

            var permissions = await _context.RolePermissions
                .Where(rp => roleIds.Contains(rp.RoleId))
                .Include(rp => rp.Permission)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync();

            return permissions;
        }
    }
}