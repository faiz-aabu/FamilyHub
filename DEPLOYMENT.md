# Production Deployment Preparation

This project is configured for production deployment with the following preparation steps already applied.

## Configuration

- `appsettings.json`
  - Does not contain connection strings, passwords, or other secrets.
  - Enables application logging with `Information` level by default.
- `appsettings.Development.json`
  - Enables automatic migrations only for local development.
  - No sensitive credentials are stored here.
- `appsettings.Production.json`
  - Does not include a production connection string.
  - Sets `SeedAdmin` to `false` for production.
  - Uses `Warning` logging level for production.
  - Contains `AllowedHosts` placeholder.

## Startup and production readiness

- `Program.cs`
  - Enables response compression for HTTPS responses.
  - Configures the SQL Server database from `ConnectionStrings:DefaultConnection`.
  - Throws a startup error if `DefaultConnection` is missing.
  - Registers identity and application services.
  - Registers the no-op email sender only in development.
  - Configures application cookies with secure policies.
  - Runs EF Core migrations at startup only when `Database:ApplyMigrationsOnStartup=true`.
  - Uses production error handling and HSTS when not in development.
  - Enables HTTPS redirection.
  - Enables static file serving.

## Sensitive settings

- Move production secrets to environment variables or a secret store.
- Recommended environment variables:
  - `ConnectionStrings__DefaultConnection`
  - `AdminUser__SeedAdmin`
  - `AdminUser__Email`
  - `AdminUser__Password`
  - `AdminUser__FullName`
  - `Database__ApplyMigrationsOnStartup` (only during a controlled migration release)
- Do not store production passwords or secrets in source control.

## Additional notes

- Local build verification was completed with `dotnet build -p:UseAppHost=false --no-restore`.
- A production environment should set `ASPNETCORE_ENVIRONMENT=Production`.
- If you want the app to seed an admin user in production, configure `AdminUser:SeedAdmin=true` and provide credentials through configuration.
- Render's local filesystem is ephemeral. Use object storage (S3, Cloudflare R2, or Azure Blob Storage) for uploads and backups that must survive redeployments.
- Production database restore is disabled by design; restore through the managed database provider instead.
