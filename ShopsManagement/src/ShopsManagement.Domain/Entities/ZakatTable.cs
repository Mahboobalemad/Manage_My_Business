namespace ShopsManagement.Domain.Entities;

/// <summary>
/// جدول الزكاة الرئيسي (مجموعة/قائمة جداول الزكاة).
/// </summary>
public class ZakatTable
{
    public int Id { get; set; }

    /// <summary>تاريخ إنشاء/تحديد جدول الزكاة</summary>
    public DateOnly Date { get; set; }

    /// <summary>اسم جدول الزكاة (مثلاً: جدول زكاة عام 2026، زكاة التجارة، إلخ)</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>ملاحظات</summary>
    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public ICollection<ZakatEntry> Entries { get; set; } = [];

    /// <summary>مجموع مبالغ الزكاة المنفقة بهذا الجدول</summary>
    public decimal TotalAmountSpent => Entries != null ? Entries.Sum(e => e.Amount) : 0m;
}
