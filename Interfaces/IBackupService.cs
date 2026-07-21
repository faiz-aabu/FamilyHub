namespace FamilyHub.Interfaces;

public interface IBackupService
{
    Task<string> CreateBackupAsync(string? backupName = null);
    Task<bool> RestoreBackupAsync(string fileName);
    Task<bool> DeleteBackupAsync(string fileName);
    Task<bool> RenameBackupAsync(string oldFileName, string newFileName);
    Task<byte[]?> DownloadBackupAsync(string fileName);
    Task<Models.BackupDetailViewModel?> GetBackupDetailsAsync(string fileName);
    Task<Models.BackupIndexViewModel> GetBackupIndexAsync(string? searchTerm = null, string? sortOrder = null);
}
