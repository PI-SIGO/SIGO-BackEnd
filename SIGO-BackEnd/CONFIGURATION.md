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
dotnet user-secrets set "Email:Host" "<smtp-host>" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:Port" "587" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:Username" "<smtp-user>" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:Password" "<smtp-password>" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:FromAddress" "nao-responda@example.com" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:FromName" "SIGO" --project SIGO\SIGO.csproj
dotnet user-secrets set "Email:UseSsl" "true" --project SIGO\SIGO.csproj
dotnet user-secrets set "PasswordRecovery:FrontendBaseUrl" "http://localhost:3000" --project SIGO\SIGO.csproj
dotnet user-secrets set "PasswordRecovery:TokenLifetimeMinutes" "30" --project SIGO\SIGO.csproj
```

Equivalent environment variables use double underscores, for example `Jwt__Key`,
`ConnectionStrings__DefaultConnection`, `Email__Host`, `Email__Username`,
`Email__Password`, `Email__FromAddress`,
`PasswordRecovery__FrontendBaseUrl` and
`PasswordRecovery__TokenLifetimeMinutes`.

`Jwt:Key` must be generated with a cryptographic random source and contain at least 32 UTF-8 bytes. Do not reuse development examples in production.

Depois de configurar a conexão, aplique as migrações antes de iniciar a API:

```powershell
dotnet ef database update --project SIGO\SIGO.csproj --startup-project SIGO\SIGO.csproj
```

A migração `AddPasswordRecoveryTokens` cria a tabela segura usada pelos links de
redefinição. O comando `dotnet run --project SIGO\SIGO.csproj -- --seed` também aplica
as migrações antes de carregar os dados locais de demonstração.

O cadastro do cliente continua direto e não exige confirmação de e-mail. SMTP é usado
somente no fluxo de recuperação de senha. Em Development, os padrões apontam para um
servidor SMTP local em `localhost:1025`; em outros ambientes, host, remetente e
credenciais devem ser fornecidos por configuração segura.

## Swagger and local API checks

Swagger is exposed only when `ASPNETCORE_ENVIRONMENT=Development`.

The OpenAPI contract uses the HTTP bearer security scheme for JWT-protected endpoints. In Swagger UI, paste only the JWT value into Authorize; do not include the `Bearer ` prefix. Anonymous endpoints such as login, direct registration, and health checks should not show a bearer requirement.

Use `SIGO/SIGO.http` for current local request samples. It includes direct registration, pre-registration, login, health checks, a protected request with `Authorization: Bearer {{AccessToken}}`, and the development-only OpenAPI JSON endpoint.
