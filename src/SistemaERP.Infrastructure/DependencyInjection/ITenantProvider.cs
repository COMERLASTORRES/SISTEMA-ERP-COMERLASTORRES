namespace SistemaERP.Infrastructure.DependencyInjection
{
    public interface ITenantProvider
    {
        Guid GetTenantId();
    }
}
