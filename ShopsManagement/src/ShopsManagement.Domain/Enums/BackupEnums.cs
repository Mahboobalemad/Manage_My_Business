namespace ShopsManagement.Domain.Enums;

public enum BackupType
{
    Automatic = 1, // تلقائي
    Manual = 2,    // يدوي
    Cloud = 3      // سحابي (Google Drive)
}

public enum BackupStatus
{
    Success = 1, // نجاح
    Failed = 2   // فشل
}
