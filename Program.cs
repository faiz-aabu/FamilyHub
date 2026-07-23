using System.IO.Compression;
using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using FamilyHub.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Information);

// Add MVC support with global anti-forgery validation
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddRazorPages();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSignalR();
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "application/javascript",
        "text/css",
        "application/json",
        "image/svg+xml"
    });
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);

builder.Services.Configure<AdminUserSettings>(builder.Configuration.GetSection("AdminUser"));

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Missing connection string 'DefaultConnection'. Configure it in appsettings.json, appsettings.Production.json, or environment variables.");
}

connectionString = EnsureSqlServerTrustConnection(connectionString);

builder.Services.AddDbContext<FamilyHubDbContext>(options =>
    options.UseSqlServer(connectionString));

// ASP.NET Core Identity with Roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<FamilyHubDbContext>()
.AddDefaultTokenProviders();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<IEmailSender, FamilyHubNoOpEmailSender>();
}

// Identity seeder
builder.Services.AddScoped<IdentitySeeder>();

// Cookie configuration
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Application services
builder.Services.AddScoped<IFamilyMemberService, FamilyMemberService>();
builder.Services.AddScoped<IFamilyRelationshipService, FamilyRelationshipService>();
builder.Services.AddScoped<IActivityLogService, ActivityLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBackupService, BackupService>();

static string EnsureSqlServerTrustConnection(string connectionString)
{
    var normalized = connectionString.Trim();
    var lower = normalized.ToLowerInvariant();

    if (!lower.Contains("encrypt="))
    {
        normalized += ";Encrypt=True";
    }

    if (!lower.Contains("trustservercertificate="))
    {
        normalized += ";TrustServerCertificate=True";
    }

    return normalized;
}

var app = builder.Build();

// Apply migrations and seed roles/admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var dbContext = services.GetRequiredService<FamilyHubDbContext>();
    dbContext.Database.Migrate();

    var startupLogger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseDiagnostics");
    try
    {
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        var pendingMigrationList = pendingMigrations.ToArray();
        startupLogger.LogInformation("Database migration diagnostic completed. Pending migrations: {PendingMigrations}", pendingMigrationList.Length == 0 ? "none" : string.Join(", ", pendingMigrationList));
        Console.WriteLine($"[DatabaseDiagnostics] Pending migrations: {(pendingMigrationList.Length == 0 ? "none" : string.Join(", ", pendingMigrationList))}");

        await LogMissingDatabaseObjectsAsync(dbContext, startupLogger);
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Database schema diagnostics failed. The application will continue using the existing migration behavior.");
        Console.WriteLine($"[DatabaseDiagnostics] Schema diagnostics failed.{Environment.NewLine}{ex}");
    }

    var identitySeeder = services.GetRequiredService<IdentitySeeder>();
    await identitySeeder.SeedAsync();
}

static async Task LogMissingDatabaseObjectsAsync(FamilyHubDbContext dbContext, ILogger logger)
{
    var connection = dbContext.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        await connection.OpenAsync();
    }

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS";

    var actualColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    await using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        actualColumns.Add($"{reader.GetString(0)}.{reader.GetString(1)}.{reader.GetString(2)}");
    }

    var missingObjects = new List<string>();
    foreach (var entityType in dbContext.Model.GetEntityTypes())
    {
        var tableName = entityType.GetTableName();
        if (string.IsNullOrWhiteSpace(tableName))
        {
            continue;
        }

        var schema = entityType.GetSchema() ?? "dbo";
        var tableObject = StoreObjectIdentifier.Table(tableName, schema);
        foreach (var property in entityType.GetProperties())
        {
            var columnName = property.GetColumnName(tableObject);
            if (!string.IsNullOrWhiteSpace(columnName) && !actualColumns.Contains($"{schema}.{tableName}.{columnName}"))
            {
                missingObjects.Add($"{schema}.{tableName}.{columnName}");
            }
        }
    }

    if (missingObjects.Count == 0)
    {
        logger.LogInformation("Database schema diagnostic passed. All EF Core mapped tables and columns exist.");
        Console.WriteLine("[DatabaseDiagnostics] All EF Core mapped tables and columns exist.");
        return;
    }

    var missing = string.Join(", ", missingObjects.Distinct(StringComparer.OrdinalIgnoreCase));
    logger.LogError("Database schema mismatch detected. Missing EF Core mapped columns: {MissingColumns}", missing);
    Console.WriteLine($"[DatabaseDiagnostics] Database schema mismatch. Missing columns: {missing}");
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var requestId = System.Diagnostics.Activity.Current?.Id ?? context.TraceIdentifier;
        var user = context.User?.Identity?.Name ?? "Anonymous";
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionMiddleware");
        logger.LogError(ex, "Unhandled exception before error page. RequestId: {RequestId}, Path: {Path}, User: {User}, ExceptionType: {ExceptionType}, Message: {Message}", requestId, context.Request.Path, user, ex.GetType().FullName, ex.Message);
        Console.WriteLine($"[GlobalExceptionMiddleware] RequestId={requestId}; Path={context.Request.Path}; User={user}; ExceptionType={ex.GetType().FullName}; Message={ex.Message}{Environment.NewLine}{ex}");
        throw;
    }
});

app.UseResponseCompression();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.MapHub<FamilyHub.Hubs.NotificationHub>("/notificationHub");

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Account}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();