using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;

namespace ShopsManagement.Domain.Services;

public interface IBackupService
{
    Task<BackupLog> CreateBackupAsync(BackupType type, string? customDestinationPath = null);
    Task<List<BackupLog>> GetBackupLogsAsync();
    Task<bool> RestoreBackupAsync(string backupFilePath);
}
