namespace ShopsManagement.Domain.Entities;

/// <summary>
/// تفاصيل الزكاة المنفقة التابعة لجدول زكاة معين.
/// </summary>
public class ZakatEntry
{
    public int Id { get; set; }

    /// <summary>معرف جدول الزكاة التابع له (اختياري للتوافق مع البيانات القديمة)</summary>
    public int? ZakatTableId { get; set; }

    /// <summary>تاريخ المنصرف/النفقة</summary>
    public DateOnly Date { get; set; }

    /// <summary>مبلغ الزكاة (يمكن أن يكون 0)</summary>
    public decimal Amount { get; set; }

    /// <summary>نوع الزكاة (يسجلها الأدمن حسب رغبته)</summary>
    public string ZakatType { get; set; } = string.Empty;

    /// <summary>ملاحظات</summary>
    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public ZakatTable? ZakatTable { get; set; }
}
