using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SistemaERP.Domain.Entities;
using SistemaERP.Infrastructure.Contexts;

namespace SistemaERP.Infrastructure.DependencyInjection;

/// <summary>
/// Seed idempotente del catálogo base de permisos (globales, sin tenant). Cada módulo
/// expone las acciones que le aplican con el formato "modulo.accion". Agregar un permiso
/// nuevo solo implica añadirlo a esta lista; el seed verifica por Code antes de insertar,
/// por lo que es seguro ejecutarlo múltiples veces (no duplica).
/// </summary>
public static class PermissionSeed
{
    // Acciones base reutilizables.
    private static readonly string[] CrudActions = { "view", "create", "edit", "delete" };
    private static readonly string[] CrudPlusActions =
        { "view", "create", "edit", "delete", "confirm", "cancel", "export", "manage" };

    // (Módulo, acciones, descripción por acción). Criterio módulo a módulo.
    private static readonly (string Module, string[] Actions)[] Modules =
    {
        ("products", CrudPlusActions),
        ("categories", CrudActions),
        ("customers", CrudActions),
        ("suppliers", CrudActions),
        ("purchases", CrudPlusActions),
        ("sales", CrudPlusActions),
        ("cashregister", new[] { "view", "open", "close", "movement" }),
        ("stockmovements", new[] { "view", "create", "export" }),
        ("users", CrudActions),
        ("roles", new[] { "view", "create", "edit", "delete", "assign" }),
        ("reports", new[] { "view", "export" }),
    };

    public static async Task SeedAsync(SistemaERPDbContext context)
    {
        // Consulta global (sin filtro multi-tenant) porque Permission es global.
        var existingCodes = new HashSet<string>(
            (await context.Permissions.IgnoreQueryFilters().Select(p => p.Code).ToListAsync()),
            StringComparer.OrdinalIgnoreCase);

        var toAdd = new List<Permission>();
        foreach (var (module, actions) in Modules)
        {
            foreach (var action in actions)
            {
                var code = $"{module}.{action}";
                if (existingCodes.Contains(code)) continue;

                toAdd.Add(new Permission
                {
                    Code = code,
                    Module = module,
                    Description = $"{Capitalize(action)} de {module}",
                });
                existingCodes.Add(code);
            }
        }

        if (toAdd.Count == 0) return;

        await context.Permissions.AddRangeAsync(toAdd);
        await context.SaveChangesAsync();
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
}
