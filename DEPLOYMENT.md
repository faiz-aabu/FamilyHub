# Production Deployment Preparation

This project is configured for production deployment with the following preparation steps already applied.

## Configuration

- `appsettings.json`
  - Contains the default database connection string for local environments.
  - Uses `Trusted_Connection=True` for local development only.
  - Does not include production secrets.
  - Enables application logging with `Information` level by default.
- `appsettings.Development.json`
  - Contains only development logging settings.
  - No sensitive credentials are stored here.
- `appsettings.Production.json`
  - Includes a placeholder production connection string.
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
  - Runs EF Core migrations automatically at startup.
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
- Do not store production passwords or secrets in source control.

## Additional notes

- Local build verification was completed with `dotnet build -p:UseAppHost=false --no-restore`.
- A production environment should set `ASPNETCORE_ENVIRONMENT=Production`.
- If you want the app to seed an admin user in production, configure `AdminUser:SeedAdmin=true` and provide credentials through configuration.
