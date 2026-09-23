namespace ShopsManagement.Domain.Entities;

/// <summary>
/// النشاط اليومي — يُسجَّل إجمالي مبيعات ومصروفات كل فرع ليومٍ واحد.
/// القاعدة المحاسبية: صافي المبيعات = TotalSales − TotalExpenses (الفواتير لا تدخل هنا).
/// </summary>
public class DailyActivity
{
    public int Id { get; set; }

    public int BranchId { get; set; }

    /// <summary>تاريخ اليوم (يكون فريداً لكل فرع)</summary>
    public DateOnly Date { get; set; }

    /// <summary>إجمالي المبيعات اليومية</summary>
    public decimal TotalSales { get; set; }

    /// <summary>إجمالي المصروفات اليومية</summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>
    /// صافي المبيعات (محسوب) = TotalSales − TotalExpenses.
    /// الفواتير لا تُحتسب ضمن الصافي — قاعدة محاسبية أساسية.
    /// </summary>
    public decimal NetSales => TotalSales - TotalExpenses;

    /// <summary>صافي الأرباح الاختياري</summary>
    public decimal? NetProfit { get; set; }

    /// <summary>
    /// الأرباح المعروضة: في حال عدم تسجيل صافي الأرباح تظل القيمة الافتراضية 0 (وليس صافي المبيعات).
    /// </summary>
    public decimal DisplayProfit => NetProfit ?? 0m;

    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public Branch Branch { get; set; } = null!;
    public ICollection<ExpenseItem> ExpenseItems { get; set; } = [];
}
