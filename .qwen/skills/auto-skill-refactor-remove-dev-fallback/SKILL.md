---
name: refactor-remove-dev-fallback
description: Removed development tenantId fallback from controller GetTenantId methods after verifying JWT provides tenant claim
source: auto-skill
extracted_at: '2026-07-16T12:08:41.614Z'
---

## Procedure

1. **Locate controllers with dev fallback**  
   - Found `ProductsController`, `CategoriesController`, `StockMovementsController`, and `TenantsController` each had a `GetTenantId` method that returned a hard‑coded GUID when the `tenantId` claim was missing or when running in Development.

2. **Strip the fallback logic**  
   - Replaced the fallback block with a single `return Guid.Empty;` after the claim parsing check.  
   - Removed the conditional `if (env.IsDevelopment())` import and the associated `using Microsoft.Extensions.Hosting;` statement from the affected controllers.

3. **Clean up usings**  
   - Removed `Microsoft.Extensions.Hosting` from the usings of `CategoriesController` since it was no longer required.

4. **Verify the change**  
   - Confirmed that each controller still compiles and that the signature of `GetTenantId` remains unchanged.  
   - Ran a build (`dotnet build SistemaERP.slnx --no-restore`) to ensure no compile‑time errors.

5. **Commit and document**  
   - The modifications are durable and now the application relies exclusively on the `tenantId` claim from the JWT for tenant resolution.  

**Result**: The development fallback has been eliminated, enforcing that all API endpoints must receive a valid `tenantId` claim, improving security and aligning behavior with production expectations.