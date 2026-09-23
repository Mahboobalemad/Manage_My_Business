namespace ShopsManagement.Domain.Entities;

/// <summary>
/// التجار / الموردون.
/// </summary>
public class Trader
{
    public int Id { get; set; }

    /// <summary>اسم التاجر</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>الموقع / العنوان</summary>
    public string? Location { get; set; }

    /// <summary>اسم النشاط التجاري (اختياري)</summary>
    public string? BusinessName { get; set; }

    /// <summary>رقم الهاتف</summary>
    public string? Phone { get; set; }

    /// <summary>الرصيد الافتتاحي (موجب = له / سالب = عليه)</summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>حالة التاجر (نشط / غير نشط)</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>ملاحظات</summary>
    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public ICollection<Invoice> Invoices { get; set; } = [];
    public ICollection<TraderTransaction> Transactions { get; set; } = [];
}
