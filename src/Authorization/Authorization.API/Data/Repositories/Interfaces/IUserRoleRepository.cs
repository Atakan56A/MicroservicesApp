using Authorization.API.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorization.API.Data.Repositories.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<IEnumerable<UserRole>> GetUserRolesByUserIdAsync(string userId, int tenantId);
        Task<bool> AssignRolesToUserAsync(string userId, int tenantId, IEnumerable<Guid> roleIds);
        Task<bool> RemoveRolesFromUserAsync(string userId, int tenantId, IEnumerable<Guid> roleIds);
    }
}