using System.Collections.Generic;
using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

// Catálogo de permisos es global (sin tenant). El service es una fachada delgada sobre
// el repositorio para respetar la arquitectura por capas en los controllers.
public interface IPermissionService
{
    Task<IReadOnlyList<Permission>> GetAllAsync();
}
