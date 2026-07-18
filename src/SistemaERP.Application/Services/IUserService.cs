using System.Threading.Tasks;
using SistemaERP.Domain.Entities;

namespace SistemaERP.Application.Services;

public interface IUserService
{
    Task<User> RegisterAsync(string tenantName, string email, string password, string fullName);
    Task<User?> ValidateCredentialsAsync(string email, string password);
}
