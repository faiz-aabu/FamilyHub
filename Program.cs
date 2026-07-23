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

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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

    var identitySeeder = services.GetRequiredService<IdentitySeeder>();
    await identitySeeder.SeedAsync();
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