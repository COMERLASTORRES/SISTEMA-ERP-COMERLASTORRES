using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Repositories;

public interface IPermissionRepository
{
    Task<IReadOnlyList<Permission>> GetAllAsync();
    Task<IReadOnlyList<Permission>> GetByIdsAsync(IEnumerable<Guid> ids);
    Task AddRangeAsync(IEnumerable<Permission> permissions);
}
