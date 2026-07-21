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

    public BackupService(FamilyHubDbContext context, IConfiguration configuration, IWebHostEnvironment environment)
    {
        _context = context;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<string> CreateBackupAsync(string? backupName = null)
    {
        var backupFolder = Path.Combine(_environment.WebRootPath, "backups");
        Directory.CreateDirectory(backupFolder);

        var fileName = string.IsNullOrWhiteSpace(backupName)
            ? $"familyhub-{DateTime.UtcNow:yyyyMMddHHmmss}.bak"
            : $"{SanitizeFileName(backupName)}.bak";

        var filePath = Path.Combine(backupFolder, fileName);
        var connectionString = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Database connection string is missing.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var query = $"BACKUP DATABASE [{databaseName}] TO DISK = '{filePath.Replace("\\", "\\\\")}' WITH FORMAT, INIT, NAME = 'FamilyHub Backup';";
        await using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();

        return fileName;
    }

    public async Task<bool> RestoreBackupAsync(string fileName)
    {
        var filePath = Path.Combine(_environment.WebRootPath, "backups", fileName);
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

        var query = $"USE [master]; ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; RESTORE DATABASE [{databaseName}] FROM DISK = '{filePath.Replace("\\", "\\\\")}' WITH REPLACE; ALTER DATABASE [{databaseName}] SET MULTI_USER;";
        await using var command = new SqlCommand(query, connection);
        await command.ExecuteNonQueryAsync();
        return true;
    }

    public async Task<bool> DeleteBackupAsync(string fileName)
    {
        var filePath = Path.Combine(_environment.WebRootPath, "backups", fileName);
        if (!File.Exists(filePath))
        {
            return false;
        }

        File.Delete(filePath);
        return true;
    }

    public async Task<bool> RenameBackupAsync(string oldFileName, string newFileName)
    {
        var oldPath = Path.Combine(_environment.WebRootPath, "backups", oldFileName);
        var newPath = Path.Combine(_environment.WebRootPath, "backups", $"{SanitizeFileName(newFileName)}.bak");
        if (!File.Exists(oldPath))
        {
            return false;
        }

        File.Move(oldPath, newPath);
        return true;
    }

    public async Task<byte[]?> DownloadBackupAsync(string fileName)
    {
        var filePath = Path.Combine(_environment.WebRootPath, "backups", fileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        return await File.ReadAllBytesAsync(filePath);
    }

    public async Task<BackupDetailViewModel?> GetBackupDetailsAsync(string fileName)
    {
        var filePath = Path.Combine(_environment.WebRootPath, "backups", fileName);
        if (!File.Exists(filePath))
        {
            return null;
        }

        var info = new FileInfo(filePath);
        return new BackupDetailViewModel
        {
            FileName = fileName,
            DisplayName = Path.GetFileNameWithoutExtension(fileName),
            CreatedBy = "Admin",
            CreatedAt = info.LastWriteTimeUtc,
            SizeBytes = info.Length,
            Status = "Ready"
        };
    }

    public async Task<BackupIndexViewModel> GetBackupIndexAsync(string? searchTerm = null, string? sortOrder = null)
    {
        var backupFolder = Path.Combine(_environment.WebRootPath, "backups");
        Directory.CreateDirectory(backupFolder);

        var files = Directory.GetFiles(backupFolder, "*.bak")
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

        return builder.ToString().Trim().Replace(" ", "-");
    }
}
