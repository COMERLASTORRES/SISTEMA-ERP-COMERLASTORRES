using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Application.Repositories;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public Task<IReadOnlyList<Permission>> GetAllAsync()
        {
            return _permissionRepository.GetAllAsync();
        }
    }
}
