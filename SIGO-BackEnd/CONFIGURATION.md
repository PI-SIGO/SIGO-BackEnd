# Local configuration

Production secrets must not live in tracked `appsettings*.json` files. Keep only non-secret defaults there. Configure secrets with environment variables, a secret manager, or ASP.NET Core user-secrets.

If a real database password or JWT signing key was committed, rotate it. Git history is not a secret store.

Required keys:

```powershell
dotnet user-secrets init --project SIGO\SIGO.csproj
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=SIGO;Username=<user>;Password=<password>;" --project SIGO\SIGO.csproj
dotnet user-secrets set "Jwt:Key" "<at-least-32-bytes-random-secret>" --project SIGO\SIGO.csproj
dotnet user-secrets set "Jwt:Issuer" "SIGO API" --project SIGO\SIGO.csproj
dotnet user-secrets set "Jwt:Audience" "SIGO Website" --project SIGO\SIGO.csproj
dotnet user-secrets set "CompartilhamentoCliente:CodigoHmacSecret" "<at-least-32-bytes-random-secret>" --project SIGO\SIGO.csproj
```

Equivalent environment variables use double underscores, for example `Jwt__Key` and `ConnectionStrings__DefaultConnection`.

`Jwt:Key` must be at least 32 UTF-8 bytes. Generate it with a cryptographic random source; do not reuse development examples in production.

## Swagger and local API checks

Swagger is exposed only when `ASPNETCORE_ENVIRONMENT=Development`.

The OpenAPI contract uses the HTTP bearer security scheme for JWT-protected endpoints. In Swagger UI, paste only the JWT value into Authorize; do not include the `Bearer ` prefix. Anonymous endpoints such as login, public registration, and health checks should not show a bearer requirement.

Use `SIGO/SIGO.http` for current local request samples. It includes anonymous health checks, anonymous login, a protected request with `Authorization: Bearer {{AccessToken}}`, and the development-only OpenAPI JSON endpoint.
