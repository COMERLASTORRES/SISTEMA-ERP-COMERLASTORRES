---
name: inventory-setup
description: Procedure to set up Clean Architecture, multi‑tenant data model, EF Core migration and CRUD controller for the Inventory module in a .NET 8 ERP backend.
source: auto-skill
extracted_at: '2026-07-06T03:15:06.549Z'
---

## Goal
Create the initial backend foundation for the **Inventario** module of a multi‑tenant ERP:
1. Define a Clean Architecture project layout.
2. Design the core data model (Tenant, Product, StockMovement).
3. Scaffold the .NET 8 solution and projects.
4. Implement the `Product` entity, `SistemaERPDbContext` with global tenant filter, and an EF Core migration.
5. Add a minimal `ProductsController` exposing CRUD endpoints that respect tenant isolation.

## Step‑by‑step approach (reusable)

### 1️⃣ Choose architecture
- **Clean Architecture** separates concerns into four projects:
  - `Api` – Web API, Swagger, authentication.
  - `Application` – use‑case services, DTOs, repository interfaces.
  - `Domain` – pure POCO entities, value objects, domain‑level rules.
  - `Infrastructure` – EF Core DbContext, repository implementations, migrations.
- This layout makes the system testable, allows swapping infrastructure (e.g., DB) and supports adding new modules without touching existing code.

### 2️⃣ Model multi‑tenant entities
- Add a `Tenant` aggregate with `Id` (Guid) and business fields (Name, TaxId, …).
- Every business‑level entity includes a **mandatory** `TenantId` foreign key.
- Implement **global query filters** in `DbContext` so EF automatically adds `WHERE TenantId = @CurrentTenant` to every query.
- Create audit fields (`CreatedAt`, `UpdatedAt`) and a base class or interface if you need them across entities.
- Example core entities for the inventory module:
  ```csharp
  public class Product { Guid Id; Guid TenantId; string Code; string Name; decimal PurchasePrice; decimal SalePrice; int Stock; int StockMinimum; bool IsActive; DateTime CreatedAt; DateTime? UpdatedAt; }
  public class StockMovement { Guid Id; Guid TenantId; Guid ProductId; int Quantity; MovementType Type; DateTime Timestamp; string? Note; }
  ```

### 3️⃣ Scaffold the solution (CLI commands)
```
# 1. Create solution
dotnet new sln -n SistemaERP

# 2. Create projects (net8.0)
 dotnet new webapi -n SistemaERP.Api -f net8.0
 dotnet new classlib -n SistemaERP.Application -f net8.0
 dotnet new classlib -n SistemaERP.Domain -f net8.0
 dotnet new classlib -n SistemaERP.Infrastructure -f net8.0

# 3. Add to solution
dotnet sln add src/SistemaERP.Api/SistemaERP.Api.csproj
dotnet sln add src/SistemaERP.Application/SistemaERP.Application.csproj
dotnet sln add src/SistemaERP.Domain/SistemaERP.Domain.csproj
dotnet sln add src/SistemaERP.Infrastructure/SistemaERP.Infrastructure.csproj

# 4. Wire project references
dotnet add src/SistemaERP.Api/SistemaERP.Api.csproj reference src/SistemaERP.Application/SistemaERP.Application.csproj
dotnet add src/SistemaERP.Api/SistemaERP.Api.csproj reference src/SistemaERP.Infrastructure/SistemaERP.Infrastructure.csproj
dotnet add src/SistemaERP.Application/SistemaERP.Application.csproj reference src/SistemaERP.Domain/SistemaERP.Domain.csproj
dotnet add src/SistemaERP.Infrastructure/SistemaERP.Infrastructure.csproj reference src/SistemaERP.Application/SistemaERP.Application.csproj
dotnet add src/SistemaERP.Infrastructure/SistemaERP.Infrastructure.csproj reference src/SistemaERP.Domain/SistemaERP.Domain.csproj

# 5. Add NuGet packages
# Api
 dotnet add src/SistemaERP.Api package Microsoft.AspNetCore.Authentication.JwtBearer
 dotnet add src/SistemaERP.Api package Swashbuckle.AspNetCore
# Infrastructure
 dotnet add src/SistemaERP.Infrastructure package Microsoft.EntityFrameworkCore
 dotnet add src/SistemaERP.Infrastructure package Microsoft.EntityFrameworkCore.Design
 dotnet add src/SistemaERP.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
```

### 4️⃣ Implement the Inventory core
- **Domain** → place `Product.cs` and `StockMovement.cs` under `Domain/Entities`.
- **Infrastructure** → create `SistemaERPDbContext.cs`:
  - Register `DbSet<Product>` and `DbSet<StockMovement>`.
  - Inject a `ITenantProvider` (or use `IHttpContextAccessor`) to obtain the current tenant id.
  - Apply `modelBuilder.Entity<Product>().HasQueryFilter(p => p.IsActive && p.TenantId == _currentTenantId);` (repeat for other entities).
  - Override `SaveChanges`/`SaveChangesAsync` to fill audit fields.
- **DependencyInjection** → expose an extension method `AddInfrastructureServices(connectionString)` that registers the DbContext and the `DbInitializer` (calls `context.Database.MigrateAsync()`).

### 5️⃣ EF Core migration (first run)
```bash
# install ef tool if missing
dotnet tool install --global dotnet-ef
# create migration from the root (startup project is the Api)
 dotnet ef migrations add InitInventory \
   --project src/SistemaERP.Infrastructure \
   --startup-project src/SistemaERP.Api \
   --output-dir Migrations
# apply to DB
 dotnet ef database update --project src/SistemaERP.Infrastructure --startup-project src/SistemaERP.Api
```
The generated SQL creates `Products` and `StockMovements` with composite indexes `(TenantId, Code)` and `(TenantId, ProductId)` to guarantee fast look‑ups per tenant.

### 6️⃣ Minimal CRUD controller (Api layer)
Create `Controllers/ProductsController.cs` inside the Api project:
- Constructor receives `SistemaERPDbContext` (or an `IAsyncRepository<Product>` for a more decoupled version). 
- `Get` supports pagination (`page`, `pageSize`).
- `Get/{id}` returns a single product filtered by tenant.
- `Post` reads the JWT claim `tenantId`, forces the entity’s `TenantId` to that value, validates, saves.
- `Put/{id}` loads the owned product, maps allowed fields, saves.
- `Delete/{id}` removes the entity (soft‑delete could be used instead of physical delete).
- Helper `GetTenantId()` extracts the claim and returns `Guid.Empty` if missing (the controller then returns a 400).

### 7️⃣ Security & tenant middleware (Api startup)
In `Program.cs`:
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => { /* configure authority, key, etc. */ });

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>(); // implementation reads User claim
```
Add `app.UseAuthentication(); app.UseAuthorization();` before mapping controllers.

### 8️⃣ Verify the flow
1. Start the API (`dotnet run` in `SistemaERP.Api`).
2. Open Swagger (`/swagger`) – you’ll see the `Products` endpoints.
3. Call `POST /api/Products` with a valid JWT that contains `tenantId`. The product should be persisted with that tenant id. Subsequent `GET` calls will only return rows belonging to the same tenant.

## Why this method works repeatedly
- **Explicit scaffolding commands** guarantee reproducibility across machines.
- **Global query filter + claim‑based tenant extraction** removes the risk of accidental cross‑tenant data leaks.
- **Separate migration project** keeps database versioning inside `Infrastructure`, independent of the API.
- **Minimal controller** gives a working API fast; later you can replace the direct DbContext usage with a proper Application‑layer service without touching the endpoint signatures.

## Reuse checklist for any new module (e.g., Ventas, Compras)
1. Add domain entities with `TenantId` and audit fields.
2. Register them in `DbContext` (add `DbSet<>`).
3. Run `dotnet ef migrations add <ModuleName>Init` and `dotnet ef database update`.
4. Create a controller (or Application service) that follows the same pattern of extracting `tenantId` from JWT and using the global filter.
5. Add any module‑specific business rules in the `Application` layer (e.g., tax calculation, stock reservation).

---
*This skill captures the complete, repeatable workflow used to bootstrap the Inventory module for the multi‑tenant ERP backend described in the conversation.*