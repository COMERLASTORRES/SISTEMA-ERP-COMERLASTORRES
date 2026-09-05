using System.Threading.Tasks;

namespace SistemaERP.Application.Services;

// Abstracción de transacción para coordinar operaciones atómicas entre varios
// repositorios sin que la capa Application referencie directamente Infrastructure.
public interface IUnitOfWork
{
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task SaveChangesAsync();
}
