using System.Data;
using System.Globalization;
using System.Text;
using FamilyHub.Data;
using FamilyHub.Interfaces;
using FamilyHub.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace FamilyHub.Services;

public class BackupService : IBackupService
{
    private readonly FamilyHubDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;
    private readonly string _backupFolder;

    public BackupService(FamilyHubDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
        _backupFolder = Path.Combine(_environment.ContentRootPath, "App_Data", "backups");
    }

    public async Task<string> CreateBackupAsync(string? backupName = null)
    {
        Directory.CreateDirectory(_backupFolder);

        var fileName = string.IsNullOrWhiteSpace(backupName)
            ? $"familyhub-{DateTime.UtcNow:yyyyMMddHHmmss}.bak"
            : $"{SanitizeFileName(backupName)}.bak";

        var filePath = GetBackupPath(fileName);
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = $"BACKUP DATABASE {QuoteIdentifier(databaseName)} TO DISK = N'{EscapeSqlLiteral(filePath)}' WITH FORMAT, INIT, NAME = N'FamilyHub Backup';";
        await using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();

        return fileName;
    }

    public async Task<bool> RestoreBackupAsync(string fileName)
    {
        if (!_environment.IsDevelopment())
        {
            throw new InvalidOperationException("Database restore is disabled outside development. Restore the database through your managed database provider.");
        }

        var filePath = GetBackupPath(fileName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return false;
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var quotedDatabaseName = QuoteIdentifier(databaseName);
        var query = $"USE [master]; ALTER DATABASE {quotedDatabaseName} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE {quotedDatabaseName} FROM DISK = N'{EscapeSqlLiteral(filePath)}' WITH REPLACE; ALTER DATABASE {quotedDatabaseName} SET MULTI_USER;";
        await using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();
        return true;
    }

    public Task<bool> DeleteBackupAsync(string fileName)
    {
        var filePath = GetBackupPath(fileName);
        if (!File.Exists(filePath))
        {
            return Task.FromResult(false);
        }

        File.Delete(filePath);
        return Task.FromResult(true);
    }

    public Task<bool> RenameBackupAsync(string oldFileName, string newFileName)
    {
        var oldPath = GetBackupPath(oldFileName);
        var newPath = GetBackupPath($"{SanitizeFileName(newFileName)}.bak");
        if (!File.Exists(oldPath))
        {
            return Task.FromResult(false);
        }

        File.Move(oldPath, newPath);
        return Task.FromResult(true);
    }

    public async Task<byte[]?> DownloadBackupAsync(string fileName)
    {
        var filePath = GetBackupPath(fileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    public Task<BackupDetailViewModel?> GetBackupDetailsAsync(string fileName)
    {
        var filePath = GetBackupPath(fileName);
        if (!File.Exists(filePath))
        {
            return Task.FromResult<BackupDetailViewModel?>(null);
        }

        var info = new FileInfo(filePath);
        return Task.FromResult<BackupDetailViewModel?>(new BackupDetailViewModel
        {
            FileName = fileName,
            DisplayName = Path.GetFileNameWithoutExtension(fileName),
            CreatedBy = "Admin",
            CreatedAt = info.LastWriteTimeUtc,
            SizeBytes = info.Length,
            Status = "Ready"
        });
    }

    public async Task<BackupIndexViewModel> GetBackupIndexAsync(string? searchTerm = null, string? sortOrder = null)
    {
        Directory.CreateDirectory(_backupFolder);

        var files = Directory.GetFiles(_backupFolder, "*.bak")
            .Select(path => new FileInfo(path))
            .Select(file => new BackupFileInfo
            {
                FileName = file.Name,
                DisplayName = Path.GetFileNameWithoutExtension(file.Name),
                CreatedBy = "Admin",
                CreatedAt = file.LastWriteTimeUtc,
                SizeBytes = file.Length,
                Status = "Ready"
            })
            .ToList();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            files = files.Where(item => item.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) || item.FileName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        files = sortOrder switch
        {
            "oldest" => files.OrderBy(item => item.CreatedAt).ToList(),
            "largest" => files.OrderByDescending(item => item.SizeBytes).ToList(),
            "smallest" => files.OrderBy(item => item.SizeBytes).ToList(),
            _ => files.OrderByDescending(item => item.CreatedAt).ToList()
        };

        var databaseSize = await GetDatabaseSizeAsync();

        return new BackupIndexViewModel
        {
            SearchTerm = searchTerm ?? string.Empty,
            SortOrder = sortOrder ?? "newest",
            LastBackupDate = files.FirstOrDefault()?.CreatedAt,
            TotalBackupFiles = files.Count,
            DatabaseSizeDisplay = FormatSize(databaseSize),
            StorageUsedDisplay = FormatSize(files.Sum(item => item.SizeBytes)),
            Backups = files
        };
    }

    private async Task<long> GetDatabaseSizeAsync()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return 0;
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand("SELECT CAST(SUM(size) * 8 / 1024 AS bigint) FROM sys.database_files", connection);
        var result = await command.ExecuteScalarAsync();
        return result is DBNull or null ? 0 : Convert.ToInt64(result);
    }

    private static string FormatSize(long bytes)
    {
        const int scale = 1024;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        double size = bytes;
        var unitIndex = 0;

        while (size >= scale && unitIndex < units.Length - 1)
        {
            size /= scale;
            unitIndex++;
        }

        return unitIndex == 0 ? $"{size:F0} {units[unitIndex]}" : $"{size:F1} {units[unitIndex]}";
    }

    private static string SanitizeFileName(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var builder = new StringBuilder(input);
        foreach (var invalidChar in invalidChars)
        {
            builder.Replace(invalidChar.ToString(), string.Empty);
        }

        var sanitized = builder.ToString().Trim().Replace(" ", "-");
        if (string.IsNullOrWhiteSpace(sanitized) || sanitized is "." or "..")
        {
            throw new InvalidOperationException("A valid backup name is required.");
        }

        return sanitized;
    }

    private string GetBackupPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName)
            || !string.Equals(Path.GetExtension(fileName), ".bak", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
            || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new InvalidOperationException("Invalid backup file name.");
        }

        Directory.CreateDirectory(_backupFolder);
        var path = Path.GetFullPath(Path.Combine(_backupFolder, fileName));
        var root = Path.GetFullPath(_backupFolder) + Path.DirectorySeparatorChar;
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid backup file path.");
        }

        return path;
    }

    private static string QuoteIdentifier(string identifier) => $"[{identifier.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeSqlLiteral(string value) => value.Replace("'", "''", StringComparison.Ordinal);
}
