namespace ShopsManagement.Domain.Models;

/// <summary>
/// ملخص بيانات الفرع الموحّد مع حسابات الأرصدة.
/// </summary>
public class BranchSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? RecipientName { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; }
    public decimal OpeningBalance { get; set; }

    // ====== الأرصدة المحسوبة ======

    /// <summary>صافي المبيعات للفرع (إجمالي المبيعات - إجمالي المصروفات)</summary>
    public decimal NetSalesTotal { get; set; }

    /// <summary>إجمالي الفواتير المستلمة المرتبطة بالفرع</summary>
    public decimal TotalInvoices { get; set; }

    /// <summary>إجمالي صافي مستحقات الموظفين الإيجابية (الموظفون الذين يستحقون مالاً)</summary>
    public decimal EmployeesPositiveBalance { get; set; }

    /// <summary>إجمالي صافي مستحقات الموظفين السالبة (الموظفون المدينون)</summary>
    public decimal EmployeesNegativeBalance { get; set; }

    /// <summary>إجمالي الرصيد له = الرصيد الافتتاحي إن كان موجباً + صافي المبيعات + مستحقات الموظفين الموجبة</summary>
    public decimal TotalCredit =>
        (OpeningBalance > 0 ? OpeningBalance : 0)
        + NetSalesTotal
        + EmployeesPositiveBalance;

    /// <summary>إجمالي الرصيد عليه = الرصيد الافتتاحي إن كان سالباً + إجمالي الفواتير + ديون الموظفين</summary>
    public decimal TotalDebit =>
        (OpeningBalance < 0 ? Math.Abs(OpeningBalance) : 0)
        + TotalInvoices
        + EmployeesNegativeBalance;

    /// <summary>صافي رصيد المحل = الرصيد له - الرصيد عليه</summary>
    public decimal NetBalance => TotalCredit - TotalDebit;

    /// <summary>نص العرض للحالة</summary>
    public string StatusText => IsActive ? "نشط" : "غير نشط";

    /// <summary>صافي الرصيد سالب؟ (لتحديد لون العرض)</summary>
    public bool IsNegativeNet => NetBalance < 0;

    /// <summary>نص عرض صافي الرصيد</summary>
    public string NetBalanceDisplay => $"{NetBalance:N2} ر.ي";
}
