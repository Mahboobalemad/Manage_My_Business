namespace ShopsManagement.Domain.Entities;

/// <summary>
/// تفاصيل مصروف اليوم (اختياري تفصيلي).
/// </summary>
public class ExpenseItem
{
    public int Id { get; set; }

    public int DailyActivityId { get; set; }

    /// <summary>تصنيف المصروف (مثلاً: كهرباء، نقل، ضيافة، إلخ)</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>المبلغ</summary>
    public decimal Amount { get; set; }

    // ====== Navigation Properties ======
    public DailyActivity DailyActivity { get; set; } = null!;
}
