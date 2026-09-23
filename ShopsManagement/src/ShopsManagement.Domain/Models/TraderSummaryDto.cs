namespace ShopsManagement.Domain.Models;

public class TraderSummaryDto
{
    public int TraderId { get; set; }
    public string TraderName { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? BusinessName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public decimal OpeningBalance { get; set; }

    /// <summary>إجمالي الفواتير له</summary>
    public decimal TotalInvoices { get; set; }

    /// <summary>إجمالي الدفعات المسددة عليه</summary>
    public decimal TotalPayments { get; set; }

    /// <summary>صافي الرصيد المتبقي له/عليه = الرصيد الافتتاحي + إجمالي الفواتير − إجمالي الدفعات</summary>
    public decimal NetBalance => OpeningBalance + TotalInvoices - TotalPayments;

    /// <summary>الرصيد المتبقي (مستعار لـ Backward Compatibility)</summary>
    public decimal OutstandingBalance => NetBalance;

    public int InvoicesCount { get; set; }
    public int TransactionsCount { get; set; }

    /// <summary>هل صافي الرصيد بالسالب؟ (لتحديد ألوان العرض)</summary>
    public bool IsNegativeNet => NetBalance < 0;

    /// <summary>نص عرض المتبقي له/عليه</summary>
    public string NetBalanceDisplayText
    {
        get
        {
            if (NetBalance > 0)
                return $"+{NetBalance:N2} ر.ي (له)";
            else if (NetBalance < 0)
                return $"{NetBalance:N2} ر.ي (عليه)";
            else
                return "0.00 ر.ي";
        }
    }

    /// <summary>نص حالة التاجر</summary>
    public string StatusText => IsActive ? "نشط" : "غير نشط";
}
