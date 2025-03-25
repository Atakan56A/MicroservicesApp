using Authorization.API.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorization.API.Data.Repositories.Interfaces
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role> GetRoleWithPermissionsAsync(Guid roleId);
        Task<IEnumerable<Role>> GetRolesByUserIdAsync(Guid userId, Guid tenantId);
        Task AssignPermissionsToRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds);
        Task RemovePermissionsFromRoleAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    }
}