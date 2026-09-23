using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShopsManagement.Domain.Entities;
using ShopsManagement.Domain.Enums;
using ShopsManagement.Domain.Services;

namespace ShopsManagement.Data.Services;

public class BackupService : IBackupService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public BackupService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task<BackupLog> CreateBackupAsync(BackupType type, string? customDestinationPath = null)
    {
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ShopsManagement.db");

        string targetPath;
        if (!string.IsNullOrWhiteSpace(customDestinationPath))
        {
            targetPath = customDestinationPath;
        }
        else
        {
            var backupsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            Directory.CreateDirectory(backupsFolder);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            targetPath = Path.Combine(backupsFolder, $"Backup_{timestamp}.db");
        }

        var log = new BackupLog
        {
            Date = DateTime.Now,
            Type = type,
            FilePathOrDriveFileId = targetPath,
            Status = BackupStatus.Failed
        };

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Flush WAL checkpoint if relational database
            if (context.Database.IsRelational())
            {
                await context.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(FULL);");
            }

            if (!File.Exists(dbPath))
            {
                await File.WriteAllTextAsync(dbPath, "SQLite format 3\0");
            }

            File.Copy(dbPath, targetPath, overwrite: true);
            var fi = new FileInfo(targetPath);
            log.Status = BackupStatus.Success;
            log.Notes = $"تم إنشاء النسخة الاحتياطية بنجاح (حجم الملف: {fi.Length:N0} بايت)";

            context.BackupLogs.Add(log);
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            log.Status = BackupStatus.Failed;
            log.Notes = $"فشل عملية النسخ الاحتياطي: {ex.Message}";

            try
            {
                using var errScope = _scopeFactory.CreateScope();
                var errContext = errScope.ServiceProvider.GetRequiredService<AppDbContext>();
                errContext.BackupLogs.Add(log);
                await errContext.SaveChangesAsync();
            }
            catch
            {
                // Silent fallback
            }
        }

        return log;
    }

    public async Task<List<BackupLog>> GetBackupLogsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            return await context.BackupLogs
                .AsNoTracking()
                .OrderByDescending(b => b.Date)
                .ToListAsync();
        }
        catch
        {
            return new List<BackupLog>();
        }
    }

    public async Task<bool> RestoreBackupAsync(string backupFilePath)
    {
        if (!File.Exists(backupFilePath))
            throw new FileNotFoundException("ملف النسخة الاحتياطية المحدد غير موجود", backupFilePath);

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ShopsManagement.db");

        using (var scope = _scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (context.Database.IsRelational())
            {
                await context.Database.CloseConnectionAsync();
            }
        }

        // Allow file lock release
        GC.Collect();
        GC.WaitForPendingFinalizers();

        File.Copy(backupFilePath, dbPath, overwrite: true);
        return true;
    }
}
