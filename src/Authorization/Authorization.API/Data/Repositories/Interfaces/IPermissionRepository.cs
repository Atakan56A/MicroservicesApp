using Authorization.API.Data.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Authorization.API.Data.Repositories.Interfaces
{
    public interface IPermissionRepository : IRepository<Permission>
    {
        Task<IEnumerable<Permission>> GetPermissionsByRoleIdAsync(Guid roleId);
        Task<IEnumerable<Permission>> GetUserPermissionsAsync(Guid userId, Guid tenantId);
    }
}