namespace ShopsManagement.Domain.Entities;

/// <summary>
/// الفروع — يمثّل فرعاً تجارياً واحداً.
/// </summary>
public class Branch
{
    public int Id { get; set; }

    /// <summary>اسم الفرع</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>عنوان الفرع</summary>
    public string? Address { get; set; }

    /// <summary>اسم مستلم / مسؤول المحل</summary>
    public string? RecipientName { get; set; }

    /// <summary>رقم الهاتف</summary>
    public string? Phone { get; set; }

    /// <summary>الرصيد الافتتاحي (موجب = له / سالب = عليه)</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>حالة الفرع (نشط / غير نشط)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>ملاحظات اختيارية</summary>
    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public ICollection<DailyActivity> DailyActivities { get; set; } = [];
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<Employee> Employees { get; set; } = [];
}
