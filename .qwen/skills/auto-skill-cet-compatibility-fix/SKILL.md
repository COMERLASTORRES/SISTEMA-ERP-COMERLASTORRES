---
name: CET Compatibility Fix
description: Disable CET to resolve build errors on Windows
source: auto-skill
extracted_at: '2026-07-06T03:31:13.873Z'
---

**Rule:** When encountering Windows CET compatibility errors during .NET 8 builds, add `<EnableCET>false</EnableCET>` to the `<PropertyGroup>` of all `.csproj` files and ensure the target framework is `net8.0`.

**Why:** CET (Control-flow Enforcement Technology) is not fully supported on some Windows configurations; disabling it prevents the fatal Roslyn compiler error "Your Windows doesn't fully support CET. Please install all available Windows updates."

**How to apply:**
1. Open each project file (`.csproj`) in the solution.
2. Add `<EnableCET>false</EnableCET>` inside the `<PropertyGroup>` element (create the element if missing).
3. Verify that `<TargetFramework>` is set to `net8.0`.
4. Save changes and rebuild the solution (`dotnet build`).
5. If the error persists, run `dotnet clean` then `dotnet restore` before rebuilding.

**Example PropertyGroup snippet:**
```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <EnableCET>false</EnableCET>
</PropertyGroup>
```

**Scope:** This fix applies to all projects in the solution (API, Domain, Application, Infrastructure) to ensure consistent CET disabling across the build pipeline.