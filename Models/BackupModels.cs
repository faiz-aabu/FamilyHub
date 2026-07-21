namespace FamilyHub.Models;

public class BackupFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string SizeDisplay => FormatSize(SizeBytes);
    public string Status { get; set; } = "Ready";

    public static string FormatSize(long bytes)
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
}

public class BackupIndexViewModel
{
    public string SearchTerm { get; set; } = string.Empty;
    public string SortOrder { get; set; } = "newest";
    public DateTime? LastBackupDate { get; set; }
    public int TotalBackupFiles { get; set; }
    public string DatabaseSizeDisplay { get; set; } = "0 B";
    public string StorageUsedDisplay { get; set; } = "0 B";
    public List<BackupFileInfo> Backups { get; set; } = [];
}

public class BackupDetailViewModel
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public string SizeDisplay => BackupFileInfo.FormatSize(SizeBytes);
    public string Status { get; set; } = "Ready";
    public string? Notes { get; set; }
}
