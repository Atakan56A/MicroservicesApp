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
    public class UserRoleRepository : Repository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository(AuthorizationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<UserRole>> GetUserRolesByUserIdAsync(string userId, int tenantId)
        {
            return await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                .Include(ur => ur.Role)
                .ToListAsync();
        }

        public async Task<bool> AssignRolesToUserAsync(string userId, int tenantId, IEnumerable<Guid> roleIds)
        {
            try
            {
                // Remove existing roles first
                var existingRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                    .ToListAsync();

                _context.UserRoles.RemoveRange(existingRoles);

                // Add new roles
                foreach (var roleId in roleIds)
                {
                    await _context.UserRoles.AddAsync(new UserRole
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        RoleId = roleId,
                        TenantId = tenantId,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveRolesFromUserAsync(string userId, int tenantId, IEnumerable<Guid> roleIds)
        {
            try
            {
                // Remove existing roles first
                var existingRoles = _context.UserRoles
                    .Where(ur => ur.UserId == userId && ur.TenantId == tenantId)
                    .ToList();

                _context.UserRoles.RemoveRange(existingRoles);

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}