using ShopsManagement.Domain.Enums;

namespace ShopsManagement.Domain.Entities;

/// <summary>
/// حركات حساب التاجر (دفتر Ledger التاجر).
/// رصيد التاجر المستحق = مجموع الفواتير (Invoice) − مجموع الدفعات (Payment).
/// </summary>
public class TraderTransaction
{
    public int Id { get; set; }

    public int TraderId { get; set; }

    /// <summary>تاريخ الحركة</summary>
    public DateOnly Date { get; set; }

    /// <summary>نوع الحركة: فاتورة / دفعة</summary>
    public TraderTransactionType Type { get; set; }

    /// <summary>المبلغ</summary>
    public decimal Amount { get; set; }

    /// <summary>رابط اختيار الفاتورة المتصلة للحركة إن وجدت (nullable)</summary>
    public int? RelatedInvoiceId { get; set; }

    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public Trader Trader { get; set; } = null!;
    public Invoice? RelatedInvoice { get; set; }
}
