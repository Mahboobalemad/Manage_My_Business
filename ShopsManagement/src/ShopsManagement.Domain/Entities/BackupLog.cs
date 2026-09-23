using ShopsManagement.Domain.Enums;

namespace ShopsManagement.Domain.Entities;

/// <summary>
/// سجل النسخ الاحتياطي (محلي / سحابي).
/// </summary>
public class BackupLog
{
    public int Id { get; set; }

    /// <summary>تاريخ ووقت عملية النسخ</summary>
    public DateTime Date { get; set; }

    /// <summary>نوع النسخ: محلي / سحابي</summary>
    public BackupType Type { get; set; }

    /// <summary>مسار الملف المحلي أو المعرّف في Google Drive (DriveFileId)</summary>
    public string FilePathOrDriveFileId { get; set; } = string.Empty;

    /// <summary>حالة العملية: نجاح / فشل</summary>
    public BackupStatus Status { get; set; }

    /// <summary>تفاصيل إضافية / رسالة الخطأ إن وجد</summary>
    public string? Notes { get; set; }

    // ====== Helper Display Properties ======
    public string TypeDisplay => Type switch
    {
        BackupType.Automatic => "تلقائي 🤖",
        BackupType.Manual    => "يدوي 👤",
        BackupType.Cloud     => "سحابي ☁️",
        _ => Type.ToString()
    };

    public string StatusDisplay => Status == BackupStatus.Success ? "ناجحة ✅" : "فاشلة ❌";

    public bool IsFailed => Status == BackupStatus.Failed;
}
