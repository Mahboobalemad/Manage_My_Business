namespace ShopsManagement.Domain.Models;

public class EmployeeSummaryDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? BranchName { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal OpeningBalance { get; set; }

    public decimal TotalSalaries { get; set; }
    public decimal TotalBonuses { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public decimal TotalDeductions { get; set; }
    public decimal TotalPayments { get; set; }

    /// <summary>إجمالي المستحقات الماليّة (الرصيد الافتتاحي إن كان له + الرواتب + المكافآت)</summary>
    public decimal TotalEntitlements => (OpeningBalance > 0 ? OpeningBalance : 0) + TotalSalaries + TotalBonuses;

    /// <summary>إجمالي المسحوبات والاستقطاعات (الرصيد الافتتاحي إن كان عليه + السحبيات + الخصومات + الدفعات)</summary>
    public decimal TotalWithdrawalsAndDeductions => (OpeningBalance < 0 ? Math.Abs(OpeningBalance) : 0) + TotalWithdrawals + TotalDeductions + TotalPayments;

    /// <summary>صافي المستحق النهائي (موجب = له / سالب = عليه)</summary>
    public decimal NetEntitlement => OpeningBalance + TotalSalaries + TotalBonuses - (TotalWithdrawals + TotalDeductions + TotalPayments);

    public string NetEntitlementDisplayText
    {
        get
        {
            if (NetEntitlement > 0)
                return $"+{NetEntitlement:N2} ر.ي (له)";
            else if (NetEntitlement < 0)
                return $"{NetEntitlement:N2} ر.ي (عليه)";
            else
                return "0.00 ر.ي";
        }
    }
}
