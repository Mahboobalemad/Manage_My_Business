using ShopsManagement.Domain.Enums;

namespace ShopsManagement.Domain.Entities;

/// <summary>
/// حركات الموظف المالية.
/// مستحق الموظف = الرواتب + المكافآت − السحبيات − الخصومات − الدفعات.
/// </summary>
public class EmployeeTransaction
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    /// <summary>تاريخ الحركة</summary>
    public DateOnly Date { get; set; }

    /// <summary>نوع الحركة (راتب/مكافأة/سحبية/خصم/دفعة)</summary>
    public EmployeeTransactionType Type { get; set; }

    /// <summary>المبلغ</summary>
    public decimal Amount { get; set; }

    public string? Notes { get; set; }

    // ====== Navigation Properties ======
    public Employee Employee { get; set; } = null!;
}
