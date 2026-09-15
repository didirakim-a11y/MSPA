# MSPA

MSPA is a lightweight printer automation project for Windows domain environments. It models the recommended cloud-print strategy described in the repository PDF: protect access with AD group membership, map printers automatically for end users, and expose a small API surface for operational tooling.

## Included components

- `src/MSPA.Agent` – printer mapping logic and domain models
- `src/MSPA.Api` – ASP.NET Core API that exposes printer assignment results
- `infrastructure/scripts/Map-PrinterShares.ps1` – PowerShell deployment script based on the domain mapping pattern
- `tests/MSPA.Tests` – validation for printer assignment logic

## Example usage

Call the API to evaluate assignments for a user:

```bash
dotnet run --project src/MSPA.Api/MSPA.Api.csproj
curl -X POST https://localhost:5001/api/printers/map \
  -H "Content-Type: application/json" \
  -d '{"userGroups": ["GG-Accounting"]}'
```

This returns the printers the user can access and the default mapped share.
