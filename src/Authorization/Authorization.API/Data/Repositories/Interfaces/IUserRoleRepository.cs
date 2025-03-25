using Authorization.API.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorization.API.Data.Repositories.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<IEnumerable<UserRole>> GetUserRolesByUserIdAsync(Guid userId, Guid tenantId);
        Task<bool> AssignRolesToUserAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);
        Task RemoveRolesFromUserAsync(Guid userId, Guid tenantId, IEnumerable<Guid> roleIds);
    }
}